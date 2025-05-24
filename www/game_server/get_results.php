<?php
header('Content-Type: application/json');

// Підключення до БД
$conn = new mysqli('localhost', 'root', '', 'game_server');
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Помилка підключення до БД']);
    exit;
}

// 1. Отримуємо/ініціалізуємо стан гри
$game_state = $conn->query("SELECT * FROM game_state LIMIT 1")->fetch_assoc();
if (!$game_state) {
    $conn->query("INSERT INTO game_state (current_round, is_round_active, results_sent, results_received) VALUES (1, 1, 0, 0)");
    $game_state = [
        'current_round' => 1,
        'is_round_active' => 1,
        'results_sent' => 0,
        'results_received' => 0
    ];
}

// 2. Якщо раунд активний - перевіряємо чи завершений
if ($game_state['is_round_active']) {
    $total_players = $conn->query("SELECT COUNT(*) as cnt FROM players")->fetch_assoc()['cnt'];
    $moves_count = $conn->query("SELECT COUNT(DISTINCT player_id) as cnt FROM player_moves WHERE round = ".$game_state['current_round'])->fetch_assoc()['cnt'];

    if ($moves_count < $total_players) {
        echo json_encode([
            'success' => false,
            'error' => 'Раунд ще не завершено',
            'players_completed' => $moves_count,
            'players_total' => $total_players,
            'round' => (int)$game_state['current_round']
        ]);
        $conn->close();
        exit;
    }

    $conn->query("UPDATE game_state SET is_round_active = 0, results_sent = 1");
    $game_state['is_round_active'] = 0;
    $game_state['results_sent'] = 1;
}

// 3. Обчислюємо результати (тільки при першому запиті)
if ($game_state['results_sent'] && $game_state['results_received'] == 0) {
    $result = $conn->query("
        SELECT pm.*, p.username 
        FROM player_moves pm
        JOIN players p ON pm.player_id = p.id
        WHERE round = ".$game_state['current_round']."
    ");

    $players = [];
    while ($row = $result->fetch_assoc()) {
        $players[$row['player_id']] = [
            'player_id' => $row['player_id'],
            'username' => $row['username'],
            'kronus' => (int)$row['kronus'],
            'lyrion' => (int)$row['lyrion'],
            'mystara' => (int)$row['mystara'],
            'eclipsia' => (int)$row['eclipsia'],
            'fiora' => (int)$row['fiora'],
            'score' => 0
        ];
    }

    // Логіка підрахунку очок
    $planets = ['kronus', 'lyrion', 'mystara', 'eclipsia', 'fiora'];
    $player_ids = array_keys($players);
    $total_players = count($player_ids);

    $scores = [];
    foreach ($player_ids as $pid) {
        $scores[$pid] = 0;
    }

    for ($i = 0; $i < $total_players; $i++) {
        for ($j = $i + 1; $j < $total_players; $j++) {
            $playerA = $players[$player_ids[$i]];
            $playerB = $players[$player_ids[$j]];

            $a_wins = 0;
            $b_wins = 0;

            foreach ($planets as $planet) {
                if ($playerA[$planet] > $playerB[$planet]) {
                    $a_wins++;
                } elseif ($playerA[$planet] < $playerB[$planet]) {
                    $b_wins++;
                }
            }

            if ($a_wins > $b_wins) {
                $scores[$playerA['player_id']] += 2;
            } elseif ($a_wins < $b_wins) {
                $scores[$playerB['player_id']] += 2;
            } else {
                $scores[$playerA['player_id']] += 1;
                $scores[$playerB['player_id']] += 1;
            }
        }
    }

    // Додаємо очки до масиву players
    foreach ($players as &$player) {
        $player_id = $player['player_id'];
        $player['score'] = $scores[$player_id];

        // Оновлюємо в БД
        $conn->query("UPDATE players SET total_score = total_score + {$player['score']} WHERE id = $player_id");

        // Отримуємо оновлений total_score з БД
        $res = $conn->query("SELECT total_score FROM players WHERE id = $player_id")->fetch_assoc();
        $player['total_score'] = (int)$res['total_score'];
    }

    file_put_contents("round_{$game_state['current_round']}_results.json", json_encode($players));
}

// 4. Відправляємо результати
$players = json_decode(file_get_contents("round_{$game_state['current_round']}_results.json"), true);
$results = [];
foreach ($players as $player) {
    $results[] = [
        'player_id' => $player['player_id'],
        'username' => $player['username'],
        'kronus' => $player['kronus'],
        'lyrion' => $player['lyrion'],
        'mystara' => $player['mystara'],
        'eclipsia' => $player['eclipsia'],
        'fiora' => $player['fiora'],
        'round_score' => $player['score'],
        'total_score' => $player['total_score']
    ];
}

// 5. Оновлюємо лічильник отримань
$conn->query("UPDATE game_state SET results_received = results_received + 1");
$results_received = $game_state['results_received'] + 1;

// 6. Перевіряємо чи всі отримали результати
$total_players = $conn->query("SELECT COUNT(*) as cnt FROM players")->fetch_assoc()['cnt'];
$is_new_round = false;
$new_round = $game_state['current_round'];

if ($results_received >= $total_players) {
    unlink("round_{$game_state['current_round']}_results.json");

    if ($game_state['current_round'] < 5) {
        $new_round = $game_state['current_round'] + 1;
        $conn->query("UPDATE game_state SET 
            current_round = $new_round,
            is_round_active = 1,
            results_sent = 0,
            results_received = 0");
        $is_new_round = true;
    } else {
        $conn->query("DELETE FROM games");
        $conn->query("DELETE FROM game_sessions");
        $conn->query("DELETE FROM game_state");
        $conn->query("DELETE FROM player_moves");
        $conn->query("DELETE FROM players");
        $conn->query("UPDATE game_sessions SET started = 0 WHERE started = 1");

        $is_new_round = false;
        $new_round = null;
    }
}

// 7. Відповідь
echo json_encode([
    'success' => true,
    'round_completed' => true,
    'round' => (int)$game_state['current_round'],
    'results' => $results,
    'is_new_round' => $is_new_round,
    'new_round' => $is_new_round ? $new_round : null,
    'timestamp' => time()
]);

$conn->close();
