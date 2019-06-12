<?php

// Defined("BASE_PATH") or die(ACCESS_DENIED);

use \Curl\Curl;

/**
 * Class RequestBpm
 * Library untuk melakukan request web service ke BPM
 */
class RequestBpm
{
    public $debug = false; // set true if you want see the return all of method
    private $curl;
    private $loginAttempts = 0;
    private $csrf = '';

    /**
     * Method __construct
     * Open curl
     */
    public function __construct() {
        $this->curl = new Curl();
    }

    /**
     * Method request
     * Process call web service bpm
     * @param array $endpoint endpoint web service
     *              $endpoint['type'] - required for all type request
     *              $endpoint['url'] - optional for custome url
     *              $endpoint['service'] - required for all type request
     *              $endpoint['method'] - optional for type data service, and required for rest service
     * @param array $param data for GET or POST or else
     * @param string $requestMethod request method use for call web service
     * @param array $credentials param for doing authentication. Default False
     *              $credentials['base_api'] - base url bpmonline
     *              $credentials['UserName'] - Username bpmonline
     *              $credentials['UserPassword'] - Password bpmonline
     * @param string $format only using this if use oData, default json
     * @return array result of request 
     */
    public function request($endpoint, $param, $requestMethod = 'POST', $credentials = false, $format = 'json') {        
        $successRequest = false;
        if(isset($endpoint['type'])) {

            switch (strtolower($endpoint['type'])) {
                // only support json
                case 'rest_service':
                    $url = !isset($endpoint['url']) ? BASE_API.'0/rest/'.$endpoint['service'].'/'.$endpoint['method'] : 
                        $endpoint['url'].'0/rest/'.$endpoint['service'].'/'.$endpoint['method'];
                    break;
                
                // not finished - need improved and update in param
                case 'data_service':
                    $url = !isset($endpoint['url']) ? BASE_API.'0/DataService/json/reply/'.ucfirst(strtolower($endpoint['service'])).'Query' : 
                        $endpoint['url'].'0/DataService/json/reply/'.ucfirst(strtolower($endpoint['service'])).'Query';
                    break;

                // need improved in post, put and delete method with xml format. Json format is ready
                case 'odata':
                    $url = !isset($endpoint['url']) ? BASE_API.'0/ServiceModel/EntityDataService.svc/'.$endpoint['service'].$endpoint['method'] : 
                        $endpoint['url'].'0/ServiceModel/EntityDataService.svc/'.$endpoint['service'].$endpoint['method'];
                    
                    break;

                // rest service
                default:
                    $url = !isset($endpoint['url']) ? BASE_API.'0/rest/'.$endpoint['service'].'/'.$endpoint['method'] : 
                        $endpoint['url'].'0/rest/'.$endpoint['service'].'/'.$endpoint['method'];
                    break;
            }                                                                                                                                                                                                                                                                                                      
        }
        else {
            // header('Content-Type: application/json');
            die(json_encode(
                array(
                    'success' => false,
                    'message' => 'Error: Please check again your paramaters to access this method'
                )
            )); 
        }
        
        while ($this->loginAttempts < 3) {
            $csrf = isset($_SESSION['BPMCSRF']) ? $_SESSION['BPMCSRF'] : $this->csrf;

            if(strtolower($format) == 'json' && strtolower($endpoint['type']) == 'odata') {
                $this->curl->setHeader('Content-Type', 'application/json;odata=verbose');
                $this->curl->setHeader('Accept', 'application/json;odata=verbose');
            }
            else if(strtolower($format) == 'xml' && strtolower($endpoint['type']) == 'odata') {
                $this->curl->setHeader('Content-Type', 'application/atom+xml;type=entry');
                $this->curl->setHeader('Accept', 'application/atom+xml');

                // still developing for post, put, and delete using xml
                if(strtolower($requestMethod) !== 'get') { $param = $this->convert_paramXml($param); }

                // $custom_xml_decoder = function($response) {
                //     return $this->custom_xmlDecoder($response);
                // };
                // $this->curl->setXmlDecoder($custom_xml_decoder);
            }
            else {
                $this->curl->setHeader('Content-Type', 'application/json');
                $this->curl->setHeader('Accept', 'application/json');
            }

            $this->curl->setHeader('BPMCSRF', $csrf);
            $this->curl->setHeader('ForceUseSession', "true");
            $this->curl->setCookieJar('cookies.txt');
            $this->curl->setCookieFile('cookies.txt');

            switch (strtolower($requestMethod)) {
                case 'post':
                    $this->curl->post($url, $param);
                    break;
    
                case 'get':
                    $this->curl->get($url, $param);
                    break;
    
                case 'put':
                    $this->curl->put($url, $param);
                    break;
    
                case 'delete':
                    die('Not Support Yet');
                    break;
                
                default:
                    $this->curl->post($url, $param);
                    break;
            }
    
            if($this->curl->error) {
                $message = 'Error Request: ' . $this->curl->errorCode . ': ' . $this->curl->errorMessage . "\n";
                $response = array(
                    'success' => false,
                    'message' => $message,
                    'requestHeaders' => $this->curl->requestHeaders
                );

                $this->loginAttempts++;

                if($this->debug) {
                    echo 'Login Attempts: '.$this->loginAttempts.' <br>';
                    echo 'Response Error request: <br>';
                    echo '<pre>';
                    var_dump($response);
                    echo '</pre>';
                }

                $this->authBpm($credentials);
            }
            else {
                $response = array(
                    'success' => true,
                    'requestHeaders' => $this->curl->requestHeaders,
                    'headers' => $this->curl->responseHeaders,
                    'cookies' => $this->curl->responseCookies,
                    'body' => $this->curl->response
                );
                
                if($this->debug) {
                    echo 'Login Attempts: '.$this->loginAttempts.' <br>';
                    echo 'Response request: <br>';
                    echo '<pre>';
                    var_dump($response);
                    echo '</pre>';
                }

                $successRequest = true;
                break;
            }
        }

        if($successRequest) { return $response; }

        // header('Content-Type: application/json');
        die(json_encode($response, JSON_PRETTY_PRINT));
    }

    /**
     * Method authBpm
     * Process authentication with service bpm to get cookies and csrf
     * @param array $credentials username and password 
     *              $credentials['base_api']
     *              $credentials['UserName]
     *              $credentials['UserPassword']
     */
    private function authBpm($credentials = false) {
        $url = !$credentials ? BASE_API.'ServiceModel/AuthService.svc/Login' : $credentials['base_api'].'ServiceModel/AuthService.svc/Login';
        
        $credential = !$credentials ? json_encode(array(
            'UserName' => USERNAME_API, 
            'UserPassword' => PASSWORD_API)) : json_encode($credentials);

        $this->curl->setHeader('Accept', 'application/json');
        $this->curl->setHeader('Content-Type', 'application/json');
        $this->curl->setCookieJar('cookies.txt');
        $this->curl->setCookieFile('cookies.txt');
        $this->curl->post($url, $credential);

        if($this->curl->error) {
            $message =  'Error Authentication: ' . $this->curl->errorCode . ': ' . $this->curl->errorMessage . "\n";
            $response = array(
                'success' => false,
                'message' => $message,
                'requestHeaders' => $this->curl->requestHeaders
            );

            if($this->debug) {
                echo 'Response Error authBpm: <br>';
                echo '<pre>';
                var_dump($response);
                echo '</pre>';
                die();
            }
            else { 
                // header('Content-Type: application/json');
                die(json_encode($response, JSON_PRETTY_PRINT)); 
            }
        }
        else {
            $response = array(
                'success' => true,
                'requestHeaders' => $this->curl->requestHeaders,
                'headers' => $this->curl->responseHeaders,
                'cookies' => $this->curl->responseCookies,
                'body' => $this->curl->response,
                'csrf' => $this->curl->responseCookies['BPMCSRF']
            );

            $this->csrf = $_SESSION['BPMCSRF'] = $response['csrf'];

            if($this->debug) {
                echo 'Response authBpm: <br>';
                echo '<pre>';
                var_dump($response);
                echo '</pre>';
            }
        }
    }

    /**
     * Method convert_paramXml
     * Convert array to request xml format bpm'online
     * application/atom+xml
     * 
     * Still Developing
     * 
     * @param array $param
     * @return string result of convert 
     */
    private function convert_paramXml($param) {
        
    }

    /**
     * Method custom_xmlDecoder
     * 
     * Still Developing
     * 
     * @param object $response response body from curl
     * @return 
     */
    private function custom_xmlDecoder($response) {
        
    }

    /**
     * Method __destruct
     * Clean up and close curl every end request
     */
    public function __destruct(){
        $this->curl->close();
    }
}