#include "PinDefinitionsAndMore.h"
#include <IRremote.h>
#include <SoftwareSerial.h>

const uint16_t SAMSUNG_ADDR = 0x707;
const uint16_t SAMSUNG_POWER = 0xE6;
const uint16_t SAMSUNG_HOME = 0x79;
const uint16_t SAMSUNG_ENTER = 0x68;
const uint16_t SAMSUNG_LEFT = 0x65;
const uint16_t SAMSUNG_RIGHT = 0x62;
const uint16_t SAMSUNG_UP = 0x60;
const uint16_t SAMSUNG_DOWN = 0x61;
const uint16_t SAMSUNG_VOL_UP = 0x7;
const uint16_t SAMSUNG_VOL_DOWN = 0xB;
const uint16_t SAMSUNG_STOP = 0x46;

const int redLedPin = 12;
const int greenLedPin = 10;
const int blueLedPin = 11;
const int button1Pin = A2;
const int button2Pin = A3;
const int button3Pin = A4;
const int redButtonPin = 4;
const int blueButtonPin = 5;
const int joystickButtonPin = 2;
const int joystickRxPin = A0;
const int joystickRyPin = A1;
const int joystickThreshold = 10;

const unsigned long keepAliveTimeout = 4999;

bool clientConnected = false;
bool esp8266Init = false;
bool soundBarPowerSwitch = false;

int currentState = 0; // 0 = TV, 1 = LVP, 2 = Audio
int button1State = 0;
int button2State = 0;
int button3State = 0;
int redButtonState = 0;
int blueButtonState = 0;
int joystickButtonState = 0;
int joystickMapX = 0;
int joystickMapY = 0;
int joystickXPos = 0;
int joystickYPos = 0;
unsigned long currentMillis = 0;

/*SoftwareSerial esp8266(8, 7);
  String dataOutResult = "";
  String dataInResponse = "";
  String dataOutResponse = "";
  String joystickOutput = "";
  String joystickSendLength = "";
  String keepAliveOutput = "";
  String keepAliveSendLength = "";
  String tcpDataInOutput = "";
  String tcpDataInSendLength = "";*/

// The following variables are automatically generated using IrScrutinizer 2.3.0 and Bomaker Ondine 1 Soundbar.rmdu
// http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809

typedef uint16_t microseconds_t;
typedef uint16_t frequency_t;

inline unsigned hz2khz(frequency_t f)
{
  return f / 1000U;
}

// bomaker odine 1 soundbar raw IR signals
const microseconds_t intro_Power[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const microseconds_t repeat_Power[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const microseconds_t intro_BT[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756};
const microseconds_t repeat_BT[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const microseconds_t intro_Optical[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756};
const microseconds_t repeat_Optical[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const microseconds_t intro_up_arrow[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const microseconds_t repeat_up_arrow[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const microseconds_t intro_down_arrow[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const microseconds_t repeat_down_arrow[] PROGMEM = {9024U, 2256U, 564U, 65535U};

// End of genereated variables

// Replace values for TCP server
const String wifiSSID = "SSID";
const String wifiPassword = "SSIDPassword";
const String wifiPort = "3000";
const String connectionString = "AT+CWJAP=\"" + wifiSSID + "\",\"" + wifiPassword + "\"\r\n";
const String serverString = "AT+CIPSERVER=1," + wifiPort + "\r\n";

const bool DEBUG = true;

/*
  Red Button: Input (TV <-> Audio)
  Blue Button: Input (LVP <-> Audio)
  +-----------+-----------------------+---------------+----------------------------------------+
  |  Control  |        1 - TV         |    2 - LVP    |     3 - Audio                          |
  +-----------+-----------------------+---------------+----------------------------------------+
  | Button 1  | TV Power              | LVP Power     | Audio Power                            |
  | Button 2  | TV Back               | LVP Back      | Audio Input                            |
  | Button 3  | TV Sound script / Vol | LVP Scroll    | ""                                     |
  | J. Button | TV Enter              | LVP Enter     | ""                                     |
  | J. Stick  | TV Direction (TV Vol) | LVP Direction | Up(+)/down(-) vol                      |
  +-----------+-----------------------+---------------+----------------------------------------+
*/

void setup()
{
  Serial.begin(9600);
  // pinMode(joystickRxPin, INPUT);
  // pinMode(joystickRyPin, INPUT);
  // pinMode(joystickButtonPin, INPUT_PULLUP);
  // pinMode(button1Pin, INPUT_PULLUP);
  // pinMode(button2Pin, INPUT_PULLUP);
  // pinMode(button3Pin, INPUT_PULLUP);
  pinMode(redButtonPin, INPUT_PULLUP);
  pinMode(blueButtonPin, INPUT_PULLUP);
  pinMode(redLedPin, OUTPUT);
  pinMode(greenLedPin, OUTPUT);
  pinMode(blueLedPin, OUTPUT);
  BlueLed();
  delay(100);
  GreenLed();
  delay(100);
  RedLed();
  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK);

  if (DEBUG)
    Serial.println("Ready");
}

void loop()
{
  currentMillis = millis();

  // Separate main loop to implement simple timer
  while ((currentMillis + keepAliveTimeout) > millis())
  {
    InnerLoop();
  }

  // If TCP client has connected, send keep alive every 5s
  if (clientConnected)
  {
    // SendTcpKeepAlive();
  }
}

void InnerLoop()
{
  // button1State = digitalRead(button1Pin);
  // button2State = digitalRead(button2Pin);
  // button3State = digitalRead(button3Pin);
  redButtonState = digitalRead(redButtonPin);
  blueButtonState = digitalRead(blueButtonPin);

  if (redButtonState == 0)
  {
    if (DEBUG)
      Serial.println("redButton");
    if (currentState == 0)
    {
      currentState = 2;
      GreenLed();
    }
    else if (currentState == 2)
    {
      currentState = 0;
      RedLed();
    }
    else if (currentState = 1)
    {
      currentState = 0;
      RedLed();
    }
  }

  if (blueButtonState == 0)
  {
    if (DEBUG)
      Serial.println("blueButton");
    if (currentState == 0)
    {
      currentState = 1;
      // InitializeEsp8266();
      BlueLed();
    }
    else if (currentState == 2)
    {
      currentState = 1;
      BlueLed();
    }
    else if (currentState == 1)
    {
      currentState = 2;
      GreenLed();
    }
  }

  /*if (button1State == 0) {
    if (DEBUG) Serial.println("button1");
    }

    if (button2State == 0) {
    if (DEBUG) Serial.println("button2");
    }

    if (button3State == 0) {
    if (DEBUG) Serial.println("button3");
    }*/

  // Check for incoming data every loop with 50 ms timeout
  if (esp8266Init)
  {
    // TcpDataIn(50);
  }
  else
  {
    delay(50);
  }
}

/*
  |-------------------|

    REGION LED CONTROL

  |-------------------|
*/

void RGB_color(int redLightValue, int greenLightValue, int blueLightValue)
{
  analogWrite(redLedPin, redLightValue);
  analogWrite(greenLedPin, greenLightValue);
  analogWrite(blueLedPin, blueLightValue);
}

void RedLed()
{
  delay(150);
  RGB_color(255, 0, 0);
}

void BlueLed()
{
  delay(150);
  RGB_color(0, 0, 255);
}

void GreenLed()
{
  delay(150);
  RGB_color(0, 255, 0);
}

void TurnOffLed()
{
  RGB_color(0, 0, 0);
}

void FlashRed()
{
  TurnOffLed();
  delay(150);
  RedLed();
}

void FlashBlue()
{
  TurnOffLed();
  delay(150);
  BlueLed();
}

void FlashGreen()
{
  TurnOffLed();
  delay(150);
  GreenLed();
}

void BlinkRed()
{
  for (int i = 0; i < 5; i++)
  {
    TurnOffLed();
    delay(50);
    RedLed();
  }
}

void BlinkBlue()
{
  for (int i = 0; i < 5; i++)
  {
    TurnOffLed();
    delay(50);
    BlueLed();
  }
}

void BlinkGreen()
{
  for (int i = 0; i < 5; i++)
  {
    TurnOffLed();
    delay(50);
    GreenLed();
  }
}

/*
  |-------------------|

    ENDREGION

  |-------------------|
*/

/*
  void InnerLoop() {


  if (inputButtonPinState == 0) {
    Serial.println("input");
    if (currentState == 0) {
      currentState = 2;
      GreenLed();
    } else if (currentState == 2) {
      currentState = 0;
      RedLed();
    } else if (currentState = 1) {
      currentState = 0;
      RedLed();
    }
  }

  if (espInitButtonPinState == 0) {
    Serial.println("esp init");
    if (currentState == 0) {
      currentState = 1;
      InitializeEsp8266();
      BlueLed();
    } else if (currentState == 2) {
      currentState = 1;
      BlueLed();
    } else if (currentState == 1) {
      currentState = 2;
      GreenLed();
    }
  }

  joystickXPos = analogRead(joystickRxPin);
  joystickYPos = analogRead(joystickRyPin);
  joystickButtonPinState = digitalRead(joystickButtonPin);
  joystickMapX = map(joystickXPos, 0, 1023, -512, 512);
  joystickMapY = map(joystickYPos, 0, 1023, -512, 512);
  joystickOutput = String(joystickMapX) + "," + String(joystickMapY) + "," + String(joystickButtonPinState) + "," + String(scrollButtonPinState) + "\r\n";

  // If esp8266 not initialized joystick control sends infared tv remote signals
  if ((scrollButtonPin == LOW || backButtonPinState == LOW || joystickButtonPinState == LOW || joystickMapX > joystickThreshold || joystickMapX < -joystickThreshold
       || joystickMapY > joystickThreshold || joystickMapY < -joystickThreshold) && currentState == 0) {
    TvControl();
  }

  // Else joystick data is sent over tcp to control mouse movement
  if ((scrollButtonPin == LOW || backButtonPinState == LOW || joystickButtonPinState == LOW || joystickMapX > joystickThreshold || joystickMapX < -joystickThreshold
       || joystickMapY > joystickThreshold || joystickMapY < -joystickThreshold) && clientConnected && currentState == 1) {
    MouseControl();
  }

  // Else joystick data is sent over tcp to control mouse movement
  if ((scrollButtonPin == LOW || backButtonPinState == LOW || joystickButtonPinState == LOW || joystickMapX > joystickThreshold || joystickMapX < -joystickThreshold
       || joystickMapY > joystickThreshold || joystickMapY < -joystickThreshold) && currentState == 2) {
    SoundBarVolume();
  }

  // Check for incoming data every loop with 50 ms timeout
  if (esp8266Init) {
    //TcpDataIn(50);
  } else {
    delay(50);
  }
  }

  // https://github.com/Arduino-IRremote/Arduino-IRremote/tree/master/examples/SendRawDemo
  void sendRaw(const microseconds_t intro[], size_t lengthIntro, const microseconds_t repeat[], size_t lengthRepeat, frequency_t frequency, unsigned times) {
  if (lengthIntro > 0U) {
    IrSender.sendRaw_P(intro, lengthIntro, hz2khz(frequency));
  }
  if (lengthRepeat > 0U) {
    for (unsigned i = 0U; i < times - (lengthIntro > 0U); i++) {
      IrSender.sendRaw_P(repeat, lengthRepeat, hz2khz(frequency));
    }
  }
  }

  void PowerSoundBar() {
  if (soundBarPowerSwitch) {
    Serial.println("Power off sound bar");
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    soundBarPowerSwitch = false;
  } else {
    Serial.println("Power on sound bar");
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    soundBarPowerSwitch = true;
  }
  delay(250);
  }

void SoundBarVolume() {
  digitalWrite(redLedPin, LOW);
  if (volumeLevel < volumeTracker) {
    Serial.print("Volume down sb: ");
    Serial.print(volumeAnalogValue);
    Serial.print("  ");
    Serial.println(volumeLevel);
    sendRaw(intro_down_arrow, 68U, repeat_down_arrow, 4U, 38400U, 1);
    volumeTracker = volumeLevel;
  } else {
    Serial.print("Volume up sb: ");
    Serial.print(volumeAnalogValue);
    Serial.print("  ");
    Serial.println(volumeLevel);
    sendRaw(intro_up_arrow, 68U, repeat_up_arrow, 4U, 38400U, 1);
    volumeTracker = volumeLevel;
  }
  delay(250);
}

void OverrideSoundBarVolume() {
  digitalWrite(redLedPin, LOW);
  if (volumeLevel < 7) {
    Serial.println("Volume down (override sb)");
    sendRaw(intro_down_arrow, 68U, repeat_down_arrow, 4U, 38400U, 1);
  } else {
    Serial.println("Volume up (override sb)");
    sendRaw(intro_up_arrow, 68U, repeat_up_arrow, 4U, 38400U, 1);
  }
  delay(250);
}

void TvVolume() {
  digitalWrite(redLedPin, LOW);
  if (volumeLevel < volumeTracker) {
    Serial.print("Volume down tv: ");
    Serial.print(volumeAnalogValue);
    Serial.print("  ");
    Serial.println(volumeLevel);
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_VOL_DOWN, 0, false);
    volumeTracker = volumeLevel;
  } else {
    Serial.print("Volume up tv: ");
    Serial.print(volumeAnalogValue);
    Serial.print("  ");
    Serial.println(volumeLevel);
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_VOL_UP, 0, false);
    volumeTracker = volumeLevel;
  }
  delay(250);
}

void OverrideTvVolume() {
  digitalWrite(redLedPin, LOW);
  if (volumeLevel < 7) {
    Serial.println("Volume down (override tv)");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_VOL_DOWN, 0, false);
  } else {
    Serial.println("Volume up (override tv)");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_VOL_UP, 0, false);
  }
  delay(250);
}

  // send remote signal pattern to navigate and click on sound input settings option
  void TvSoundInput() {
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
  Serial.println("home");
  for (int i = 0; i < 20; i++) {
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_LEFT, 0, false);
    Serial.println(String(i) + " left");
  }
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
  Serial.println("right");
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_UP, 0, false);
  Serial.println("up");
  for (int i = 0; i < 3; i++) {
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
    Serial.println(String(i) + " right");
  }
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_ENTER, 0, false);
  Serial.println("enter");
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
  Serial.println("home");
  }

  void TvControl() {
  if (scrollButtonPinState == LOW) {
    Serial.println("Power tv");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_POWER, 0, false);
  } else if (backButtonPinState == LOW) {
    Serial.println("tv back");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
    // Block sending enter signal if sending stop signal
  } else if (joystickButtonPinState == LOW) {
    Serial.println("tv enter");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_ENTER, 0, false);
  } else if (joystickMapX > joystickThreshold) {
    Serial.println("tv left");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_LEFT, 0, false);
  } else if (joystickMapX < -joystickThreshold) {
    Serial.println("tv right");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
  } else if (joystickMapY > joystickThreshold) {
    Serial.println("tv up");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_UP, 0, false);
  } else if (joystickMapY < -joystickThreshold) {
    Serial.println("tv down");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_DOWN, 0, false);
  }
  FlashRed();
  }

  void MouseControl() {
  // Data sent over tcp contains joystick x/y positions, joystick buttin pin state, and scroll button pin state
  joystickOutput = String(joystickMapX) + "," + String(joystickMapY) + "," + String(joystickButtonPinState) + "," + String(scrollButtonPinState) + "\r\n";
  joystickSendLength = "AT+CIPSEND=0," + String(joystickOutput.length()) + "\r\n";
  TcpDataOut(joystickSendLength, 10);
  TcpDataOut(joystickOutput, 100);
  Serial.print(joystickOutput);
  }

  void InitializeEsp8266() {
  if (!esp8266Init) {
    esp8266.begin(9600);
    Serial.println("Starting esp8266...");
    TcpDataOut("AT+RST\r\n", 2100); // reset module
    TcpDataOut("AT+CWMODE=1\r\n", 201); // wifi mode: 1 station 2 soft access point 3 both
    dataOutResult = TcpDataOut("AT+CIFSR\r\n", 201); // get assigned IP address

    if (dataOutResult.indexOf("0.0") > 0) {
      //"AT+CWJAP=\"" + wifiSSID + "\",\"" + wifiPassword + "\"\r\n";
      //AT+CIPSERVER=1," + wifiPort + "\r\n";
      Serial.println("Invalid IP");
      dataOutResult = TcpDataOut(connectionString, 2100); // join access point
      while (!esp8266.find("OK")) {}
      if (dataOutResult.indexOf("0.0") > 0) {
        Serial.println("Invalid IP after AT+CWJAP");
        ResetEsp8266();
        return;
      }
      Serial.println("Connected to wifi");
      TcpDataOut("AT+CIFSR\r\n", 201);
    }

    TcpDataOut("AT+CIPMUX=1\r\n", 201); // enable multiple connections
    TcpDataOut(serverString, 201); // 1 for create (at port 3000), 0 for delete
    esp8266Init = true;
    Serial.println("Esp8266 ready");
  }
  }

  void ResetEsp8266() {
  BlinkBlue();
  Serial.println("Restarting esp8266...");
  InitializeEsp8266();
  Serial.println("Esp8266 ready");
  clientConnected = false;
  }

  void TcpDataIn(const int timeout) {
  dataInResponse = "";
  long int time = millis();
  while ((time + timeout) > millis()) {
    while (esp8266.available()) {
      char c = esp8266.read();
      dataInResponse += c;
    }
  }

  if (dataInResponse.length() == 0) {
    if (!clientConnected && currentState == 1) {
      FlashBlue();
    }
    return;
  }

  Serial.println("Received: " + dataInResponse);
  // Full string may get cut off due to low delay
  if (dataInResponse.indexOf("zzzz") > 0 || dataInResponse.indexOf("zzz") > 0 || dataInResponse.indexOf("zz") > 0 || dataInResponse.indexOf("z") > 0) {
    clientConnected = true;
    Serial.println("Client connected");
    tcpDataInOutput = "initack\r\n";
    tcpDataInSendLength = "AT+CIPSEND=0," + String(tcpDataInOutput.length()) + "\r\n";
    TcpDataOut(tcpDataInSendLength, 10);
    TcpDataOut(tcpDataInOutput, 100);
    clientConnected = true;
    return;
  }

  if (dataInResponse.indexOf("nlink") > 0) {
    Serial.println("Unlink detected");
    clientConnected = false;
    return;
  }
  }

  String TcpDataOut(String command, const int timeout) {
  dataOutResponse = "";
  esp8266.print(command);
  long int time = millis();
  while ((time + timeout) > millis()) {
    while (esp8266.available()) {
      char c = esp8266.read();
      dataOutResponse += c;
    }
  }

  if (dataOutResponse.indexOf("link is not") > 0) {
    Serial.println("Client disconnect tcp data out");
    clientConnected = false;
    return;
  }

  if (dataOutResponse.indexOf("Error") > 0) {
    Serial.println("Error in TcpDataOut");
    ResetEsp8266();
  }

  Serial.println(dataOutResponse);
  return dataOutResponse;
  }

  void SendTcpKeepAlive() {
  // Setup data transmission circulation every 5s to avoid esp8266 timeout mechanism (page 9/14)
  // https://www.espressif.com/sites/default/files/documentation/4b-esp8266_at_command_examples_en.pdf
  keepAliveOutput = "ka\r\n";
  keepAliveSendLength = "AT+CIPSEND=0," + String(keepAliveOutput.length()) + "\r\n";
  TcpDataOut(keepAliveSendLength, 10);
  TcpDataOut(keepAliveOutput, 100);
  //Serial.print("Send keep alive. ");
  //Serial.println("currentMillis: " + String(currentMillis) + " (+ keepAliveTimeout: " + String(currentMillis + keepAliveTimeout) + ")");
  }*/
