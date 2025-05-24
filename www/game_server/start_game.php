<?php
header('Content-Type: application/json; charset=utf-8');

// Параметри
$db_host = 'localhost';
$db_user = 'root';
$db_pass = '';
$db_name = 'game_server';
$min_players = 2;
$wait_seconds = 60;

$conn = new mysqli($db_host, $db_user, $db_pass, $db_name);
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(['error' => 'Database connection error']);
    exit;
}
$conn->set_charset("utf8mb4");

// 1. Перевірка, чи гра вже йде
$stmt = $conn->prepare("SELECT id, start_time, started FROM game_sessions ORDER BY id DESC LIMIT 1");
$stmt->execute();
$result = $stmt->get_result();

$session = null;
if ($result->num_rows > 0) {
    $session = $result->fetch_assoc();
}
$stmt->close();

// 2. Якщо гра вже почалась — повертаємо статус 'started'
if ($session && intval($session['started']) === 1) {
    echo json_encode([
        'status' => 'started',
        'elapsed_time' => time() - intval($session['start_time']),
        'message' => 'The game has already started.',
    ]);
    $conn->close();
    exit;
}

// 3. Підрахунок гравців, що чекають
$stmt = $conn->prepare("SELECT COUNT(*) as cnt FROM players WHERE status = 'waiting'");
$stmt->execute();
$result = $stmt->get_result();
$row = $result->fetch_assoc();
$player_count = intval($row['cnt']);
$stmt->close();

if ($player_count < $min_players) {
    echo json_encode([
        'status' => 'waiting',
        'time_left' => null,
        'message' => "Waiting for Players to Join. Need at least $min_players",
        'player_count' => $player_count,
    ]);
    $conn->close();
    exit;
}

// 4. Якщо немає сесії — створюємо з часом початку
if (!$session) {
    $start_time = time() + $wait_seconds;
    $stmt = $conn->prepare("INSERT INTO game_sessions (start_time, started) VALUES (?, 0)");
    $stmt->bind_param("i", $start_time);
    $stmt->execute();
    $session_id = $stmt->insert_id;
    $stmt->close();

    echo json_encode([
        'status' => 'waiting',
        'time_left' => $wait_seconds,
        'message' => "the game starts in $wait_seconds seconds.",
        'player_count' => $player_count,
    ]);
    $conn->close();
    exit;
}

// 5. Якщо сесія існує, але ще не почалась
$current_time = time();
$start_time = intval($session['start_time']);
$session_id = intval($session['id']);

if ($current_time < $start_time) {
    $time_left = $start_time - $current_time;
    echo json_encode([
        'status' => 'waiting',
        'time_left' => $time_left,
        'message' => "the game starts in $time_left seconds.",
        'player_count' => $player_count,
    ]);
    $conn->close();
    exit;
}

// 6. Якщо час вийшов — стартуємо гру
$status_playing = 'playing';
$status_waiting = 'waiting';

$stmt = $conn->prepare("UPDATE players SET status = ? WHERE status = ?");
$stmt->bind_param("ss", $status_playing, $status_waiting);
$stmt->execute();
$stmt->close();

// Позначаємо сесію як "started"
$stmt = $conn->prepare("UPDATE game_sessions SET started = 1 WHERE id = ?");
$stmt->bind_param("i", $session_id);
$stmt->execute();
$stmt->close();

echo json_encode([
    'status' => 'started',
    'elapsed_time' => $current_time - $start_time,
    'message' => 'Game started!',
    'player_count' => $player_count,
]);

$conn->close();
?>