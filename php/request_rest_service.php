<?php

require __DIR__.'/vendor/autoload.php';
require_once 'library.request_service_bpm.php';

// 1. Define class RequestBpm
$requestBpm = new RequestBpm();

// 2. Define url, and credential bpm'online
// you can using constanta for base url for api, username, and password
define('BASE_API', 'https://017109-studio.bpmonline.com/');
define('USERNAME_API', 'SystemCall');
define('PASSWORD_API', 'SystemCall');

// OR

// you can using variabel too. It's up to u, you're used to using which one 
$credentials = array(
    'base_api' => 'https://017109-studio.bpmonline.com/',
    'UserName' => 'SystemCall',
    'UserPassword' => 'SystemCall'
);

// 3. Define parameters for request

// endpoint is uri for access api
$endpoint_restService = array(
    'url' => $credentials['base_api'], // optional, you can use or not
    'type' => 'rest_service',
    'service' => 'DashboardWebService',
    'method' => 'GetAll'
);

// param for access api
    $param = array(
        'Tahun' => date('Y'),
        'Bulan' => date('n'),
        'Pic' => 'WG',
        'Stage' => 'Terendah & Terkontrak'
    );

// 4. Set debug. true for debuging, and false for default. Optional
// $requestBpm->debug = true;

// 5. Do Request
$method = 'POST';
$sendRequest = $requestBpm->request($endpoint, $param, $method, $credentials);

// 6. doing some logic for handling the return
echo '<pre>';
var_dump($sendRequest);
echo '</pre>';