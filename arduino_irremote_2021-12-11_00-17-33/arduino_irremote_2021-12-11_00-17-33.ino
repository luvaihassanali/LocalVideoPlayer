#include <Arduino.h>
#include "PinDefinitionsAndMore.h"
#include <IRremote.h>
#include <SoftwareSerial.h>

// Replace values for TCP server
const String wifiSSID = "SSID";
const String wifiPassword = "Password";
const String wifiPort = "3000";
const String connectionString = "AT+CWJAP=\"" + wifiSSID + "\",\"" + wifiPassword + "\"\r\n";
const String serverString = "AT+CIPSERVER=1," + wifiPort + "\r\n";

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

const int backButtonPin = A2;
const int blueLedPin = 5;
const int joystickRxPin = A0;
const int joystickRyPin = A1;
const int joystickButtonPin = 2;
const int joystickThreshold = 50;
const int opticalPin = 10;
const int potentiometerPin = A5;
const int redLedPin = 13;
const int scrollPin = 6;
const int soundBarPowerPin = 12;
const int soundInputPin = A4;
const int tvPowerPin = 9;
const int tvVolumePin = A3;
const int volOvPin = 11;
const unsigned long keepAliveTimeout = 4999;

int backButtonPinState = 0;
int joystickXPos = 0;
int joystickYPos = 0;
int joystickButtonPinState = 0;
int joystickMapX = 0;
int joystickMapY = 0;
int opticalPinState = 0;
int scrollPinState = 0;
int soundBarPowerPinState = 0;
int soundInputPinState = 0;
int tvPowerPinState = 0;
int tvVolumePinState = 0;
int volOvPinState = 0;
int volumeAnalogValue = 0;
int volumeLevel = 1;
int volumeTracker = 1;
unsigned long currentMillis = 0;

bool clientConnected = false;
bool esp8266Init = false;
bool opticalBluetoothSwitch = false;
bool soundBarPowerSwitch = false;
bool tvPowerSwitch = false;
bool volumeFirstRead = true;

SoftwareSerial esp8266(8, 7);
String dataOutResult = "";
String dataInResponse = "";
String dataOutResponse = "";
String joystickOutput = "";
String joystickSendLength = "";
String keepAliveOutput = "";
String keepAliveSendLength = "";
String tcpDataInOutput = "";
String tcpDataInSendLength = "";

// The following variables are automatically generated using IrScrutinizer 2.3.0 and Bomaker Ondine 1 Soundbar.rmdu
// http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809

typedef uint16_t microseconds_t;
typedef uint16_t frequency_t;

inline unsigned hz2khz(frequency_t f) {
  return f / 1000U;
}

// bomaker odine 1 soundbar raw IR signals
const microseconds_t intro_Power[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_Power[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_BT[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_BT[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_Optical[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_Optical[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_up_arrow[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_up_arrow[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_down_arrow[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_down_arrow[] PROGMEM = { 9024U, 2256U, 564U, 65535U };

void setup() {
  Serial.begin(9600);
  Serial.println("Serial ready");

  pinMode(redLedPin, OUTPUT);
  pinMode(blueLedPin, OUTPUT);
  pinMode(joystickRxPin, INPUT);
  pinMode(joystickRyPin, INPUT);

  pinMode(soundBarPowerPin, INPUT_PULLUP);
  pinMode(volOvPin, INPUT_PULLUP);
  pinMode(opticalPin, INPUT_PULLUP);
  pinMode(tvPowerPin, INPUT_PULLUP);
  pinMode(scrollPin, INPUT_PULLUP);
  pinMode(joystickButtonPin, INPUT_PULLUP);
  pinMode(backButtonPin, INPUT_PULLUP);
  pinMode(soundInputPin, INPUT_PULLUP);
  pinMode(tvVolumePin, INPUT_PULLUP);

  IrSender.begin(IR_SEND_PIN, DISABLE_LED_FEEDBACK);
  Serial.println("Infared ready");
  digitalWrite(redLedPin, HIGH);
}

void loop() {
  currentMillis = millis();

  // Separate main loop to implement simple timer
  while ((currentMillis + keepAliveTimeout) > millis()) {
    InnerLoop();
  }

  // If TCP client has connected, send keep alive every 5s
  if (clientConnected) {
    SendTcpKeepAlive();
  }
}

void InnerLoop() {
  soundBarPowerPinState = digitalRead(soundBarPowerPin);
  if (soundBarPowerPinState == LOW) {
    PowerSoundBar();
  }

  volumeAnalogValue = analogRead(potentiometerPin);
  volumeLevel = volumeAnalogValue / 64;
  // Ignore first value read at power on to initalize potentiometer position
  if (volumeFirstRead) {
    volumeTracker = volumeLevel;
    volumeFirstRead = false;
  }

  tvVolumePinState = digitalRead(tvVolumePin);
  if (tvVolumePinState == LOW) {
    // If button held then tv vol otherwise soundbar
    if (volumeLevel != volumeTracker) {
      TvVolume();
    }
    if (volOvPinState == LOW) {
      OverrideTvVolume();
    }
    // If button held and joystick button pushed send IR hub stop signal
    if (joystickButtonPinState == LOW) {
      digitalWrite(redLedPin, LOW);
      Serial.println("stop");
      IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_STOP, 0, false);
      delay(250);
    }
  }

  if (volumeLevel != volumeTracker) {
    SoundBarVolume();
  }

  volOvPinState = digitalRead(volOvPin);
  // Block sound bar volume control while controlling tv
  if (volOvPinState == LOW && tvVolumePinState == HIGH) {
    OverrideSoundBarVolume();
  }

  opticalPinState = digitalRead(opticalPin);
  if (opticalPinState == LOW) {
    SoundBarInput();
  }

  tvPowerPinState = digitalRead(tvPowerPin);
  if (tvPowerPinState == LOW) {
    digitalWrite(redLedPin, LOW);
    Serial.println("Power tv");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_POWER, 0, false);
    delay(250);
  }

  soundInputPinState = digitalRead(soundInputPin);
  if (soundInputPinState == LOW) {
    TvSoundInput();
    delay(250);
  }

  joystickXPos = analogRead(joystickRxPin);
  joystickYPos = analogRead(joystickRyPin);
  joystickButtonPinState = digitalRead(joystickButtonPin);
  joystickMapX = map(joystickXPos, 0, 1023, -512, 512);
  joystickMapY = map(joystickYPos, 0, 1023, -512, 512);
  scrollPinState = digitalRead(scrollPin);
  backButtonPinState = digitalRead(backButtonPin);

  // If esp8266 not initialized joystick control sends infared tv remote signals
  if ((backButtonPinState == LOW || joystickButtonPinState == LOW || joystickMapX > joystickThreshold || joystickMapX < -joystickThreshold
       || joystickMapY > joystickThreshold || joystickMapY < -joystickThreshold) && !esp8266Init) {
    TvControl();
  }

  // Else joystick data is sent over tcp to control mouse movement
  if ((joystickButtonPinState == LOW || scrollPinState == LOW || joystickMapX > joystickThreshold || joystickMapX < -joystickThreshold
       || joystickMapY > joystickThreshold || joystickMapY < -joystickThreshold) && clientConnected) {
    MouseControl();
  }

  // Initialize esp8266 as tcp server on scroll button press
  if (scrollPinState == LOW && !esp8266Init) {
    esp8266.begin(9600);
    Serial.println("Starting esp8266...");
    InitializeEsp8266();
    esp8266Init = true;
    Serial.println("Esp8266 ready");
  }

  // Check for incoming data every loop with 50 ms timeout
  if (esp8266Init) {
    TcpDataIn(50);
  }

  // Set blue light on after tcp connection initalized to blink after disabled by sending tcp data
  if (clientConnected) {
    digitalWrite(blueLedPin, HIGH);
  }

  // Set red light on to blink after disabled by sending IR code
  digitalWrite(redLedPin, HIGH);
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
  digitalWrite(redLedPin, LOW);
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

void SoundBarInput() {
  digitalWrite(redLedPin, LOW);
  if (opticalBluetoothSwitch) {
    Serial.println("Bluetooth");
    sendRaw(intro_BT, 68U, repeat_BT, 4U, 38400U, 1);
    opticalBluetoothSwitch = false;
  } else {
    Serial.println("Optical");
    sendRaw(intro_Optical, 68U, repeat_Optical, 4U, 38400U, 1);
    opticalBluetoothSwitch = true;
  }
  delay(250);
}

// send remote signal pattern to navigate and click on sound input settings option
void TvSoundInput() {
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
  Serial.println("home");
  for (int i = 0; i < 17; i++) {
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_LEFT, 0, false);
    Serial.println(String(i) + " left");
    FlashRedLed();
  }
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
  Serial.println("right");
  FlashRedLed();
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_UP, 0, false);
  Serial.println("up");
  FlashRedLed();
  for (int i = 0; i < 3; i++) {
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
    Serial.println(String(i) + " right");
    FlashRedLed();
  }
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_ENTER, 0, false);
  Serial.println("enter");
  FlashRedLed();
  IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
  Serial.println("home");
  FlashRedLed();
}

void FlashRedLed() {
  digitalWrite(redLedPin, LOW);
  delay(250);
  digitalWrite(redLedPin, HIGH);
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

void TvControl() {
  digitalWrite(redLedPin, LOW);
  if (backButtonPinState == LOW) {
    Serial.println("home");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_HOME, 0, false);
    // Block sending enter signal if sending stop signal
  } else if (joystickButtonPinState == LOW && tvVolumePinState == HIGH) {
    Serial.println("enter");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_ENTER, 0, false);
  } else if (joystickMapX > joystickThreshold) {
    Serial.println("left");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_LEFT, 0, false);
  } else if (joystickMapX < -joystickThreshold) {
    Serial.println("right");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_RIGHT, 0, false);
  } else if (joystickMapY > joystickThreshold) {
    Serial.println("up");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_UP, 0, false);
  } else if (joystickMapY < -joystickThreshold) {
    Serial.println("down");
    IrSender.sendSamsung(SAMSUNG_ADDR, SAMSUNG_DOWN, 0, false);
  }
  delay(250);
}

void MouseControl() {
  digitalWrite(blueLedPin, LOW);
  // Data sent over tcp contains joystick x/y positions, joystick buttin pin state, and scroll button pin state
  joystickOutput = String(joystickMapX) + "," + String(joystickMapY) + "," + String(joystickButtonPinState) + "," + String(scrollPinState) + "\r\n";
  joystickSendLength = "AT+CIPSEND=0," + String(joystickOutput.length()) + "\r\n";
  TcpDataOut(joystickSendLength, 10);
  TcpDataOut(joystickOutput, 100);
  Serial.print(joystickOutput);
}

void InitializeEsp8266() {
  FlashBlueLed();
  TcpDataOut("AT+RST\r\n", 2100); // reset module
  FlashBlueLed();
  TcpDataOut("AT+CWMODE=1\r\n", 201); // wifi mode: 1 station 2 soft access point 3 both
  FlashBlueLed();
  dataOutResult = TcpDataOut("AT+CIFSR\r\n", 201); // get assigned IP address

  if (dataOutResult.indexOf("0.0") > 0) {
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

  FlashBlueLed();
  TcpDataOut("AT+CIPMUX=1\r\n", 201); // enable multiple connections
  FlashBlueLed();
  TcpDataOut(serverString, 201); // 1 for create (at port 3000), 0 for delete
  FlashBlueLed();
}

void ResetEsp8266() {
  BlinkBlueLed();
  Serial.println("Restarting esp8266...");
  InitializeEsp8266();
  Serial.println("Esp8266 ready");
  clientConnected = false;
}

void FlashBlueLed() {
  digitalWrite(blueLedPin, HIGH);
  delay(100);
  digitalWrite(blueLedPin, LOW);
}

void BlinkBlueLed() {
  digitalWrite(blueLedPin, LOW);
  delay(100);
  digitalWrite(blueLedPin, HIGH);
  delay(100);
  digitalWrite(blueLedPin, LOW);
  delay(100);
  digitalWrite(blueLedPin, HIGH);
  delay(100);
  digitalWrite(blueLedPin, LOW);
  delay(100);
  digitalWrite(blueLedPin, HIGH);
  delay(100);
  digitalWrite(blueLedPin, LOW);
  delay(100);
  digitalWrite(blueLedPin, HIGH);
  delay(100);
  digitalWrite(blueLedPin, LOW);
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
    if (!clientConnected) {
      FlashBlueLed();
    }
    return;
  }

  Serial.println("Received: " + dataInResponse);
  // Full string may get cut off due to very low delay
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
}
