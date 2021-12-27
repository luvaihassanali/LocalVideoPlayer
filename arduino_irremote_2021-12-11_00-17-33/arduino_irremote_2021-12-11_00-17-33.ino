// This Arduino sketch was automatically generated by IrScrutinizer 2.3.0 using Bomaker Ondine 1 Soundbar.rmdu (http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809)

#include "PinDefinitionsAndMore.h"
#include <Arduino.h>
#include <IRremote.h>
#include <SoftwareSerial.h>

#define IR_CODE_NUM_BITS 32
#define DEBUG true

const int powerPin = 12;
const int potentiometerPin = A5;
const int volOvPin = 11;
const int redLedPin = 13;
const int opticalPin = 10;
const int bluetoothPin = 9;
const int scrollPin = 6;
const int blueLedPin = 5;
const int joystickRxPin = A0;
const int joystickRyPin = A1;
const int joystickButtonPin = 2;
const String ipAddr = "\"192.168.0.154\"";

IRsend irsend;
int powerPinState = 0;
int pAnalogValue = 0;
int volumeLevel = 1;
int currentRange = 1;
int volOvPinState = 0;
int opticalPinState = 0;
int bluetoothPinState = 0;
int scrollPinState = 0;

SoftwareSerial esp8266(8, 7);
String output = "";
String sendLength = "";
String result = "";
int xPosition = 0;
int yPosition = 0;
int joystickPinState = 0;
int mapX = 0;
int mapY = 0;
bool initiateInternet = true;
bool volumeFirstRead = true;

void setup() {
  Serial.begin(9600);
  if (DEBUG) {
    Serial.println("Serial ready");
  }

  pinMode(powerPin, INPUT_PULLUP);
  pinMode(volOvPin, INPUT_PULLUP);
  pinMode(opticalPin, INPUT_PULLUP);
  pinMode(bluetoothPin, INPUT_PULLUP);
  pinMode(scrollPin, INPUT_PULLUP);
  pinMode(redLedPin, OUTPUT);
  pinMode(blueLedPin, OUTPUT);
  pinMode(joystickRxPin, INPUT);
  pinMode(joystickRyPin, INPUT);
  pinMode(joystickButtonPin, INPUT_PULLUP);

  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK); // Specify send pin and enable feedback LED at default feedback LED pin
  digitalWrite(redLedPin, HIGH);

  if (DEBUG) {
    Serial.println("Infared ready (Pin " + String(IR_SEND_PIN) + ")");
  }
}

void loop() {

  powerPinState = digitalRead(powerPin);
  if (powerPinState == LOW) {
    if (DEBUG) {
      Serial.println("Power on sound bar");
    }
    digitalWrite(redLedPin, LOW);
    irsend.sendNEC(0x807F08F7U, IR_CODE_NUM_BITS);
  }

  pAnalogValue = analogRead(potentiometerPin);
  volumeLevel = pAnalogValue / 64;

  if(volumeFirstRead) {
    currentRange = volumeLevel;
    volumeFirstRead = false;
  }
  
  if (volumeLevel != currentRange) {
    if (volumeLevel < currentRange) {
      if (DEBUG) {
        Serial.print("p vol down: ");
        Serial.print(pAnalogValue);
        Serial.print("  ");
        Serial.println(volumeLevel);
      }
      digitalWrite(redLedPin, LOW);
      irsend.sendNEC(0x807F10EFU, IR_CODE_NUM_BITS);
      currentRange = volumeLevel;
    } else {
      if (DEBUG) {
        Serial.print("p vol up: ");
        Serial.print(pAnalogValue);
        Serial.print("  ");
        Serial.println(volumeLevel);
      }
      digitalWrite(redLedPin, LOW);
      irsend.sendNEC(0x807F8877U, IR_CODE_NUM_BITS);
      currentRange = volumeLevel;
    }
  }
  
  volOvPinState = digitalRead(volOvPin);
  if (volOvPinState == LOW) {
    if (volumeLevel < 7) {
      if (DEBUG) {
        Serial.println("b vol down");
      }
      digitalWrite(redLedPin, LOW);
      irsend.sendNEC(0x807F10EFU, IR_CODE_NUM_BITS);
    } else {
      if (DEBUG) {
        Serial.println("b vol up");
      }
      digitalWrite(redLedPin, LOW);
      irsend.sendNEC(0x807F8877U, IR_CODE_NUM_BITS);
    }
  }

  opticalPinState = digitalRead(opticalPin);
  if (opticalPinState == LOW) {
    if (DEBUG) {
      Serial.println("optical");
    }
    digitalWrite(redLedPin, LOW);
    irsend.sendNEC(0x807F926DU, IR_CODE_NUM_BITS);
  }

  bluetoothPinState = digitalRead(bluetoothPin);
  if (bluetoothPinState == LOW) {
    if (DEBUG) {
      Serial.println("bluetooth");
    }
    digitalWrite(redLedPin, LOW);
    irsend.sendNEC(0x807F52ADU, IR_CODE_NUM_BITS);
  }

  xPosition = analogRead(joystickRxPin);
  yPosition = analogRead(joystickRyPin);
  joystickPinState = digitalRead(joystickButtonPin);
  mapX = map(xPosition, 0, 1023, -512, 512);
  mapY = map(yPosition, 0, 1023, -512, 512);

  if (joystickPinState == 0 && initiateInternet) {
    if (DEBUG) {
      Serial.println("Starting esp8266...");
    }
    esp8266.begin(9600);
    esp8266Data("AT+RST\r\n", 2000); //reset module
    esp8266Data("AT+CWMODE=3\r\n", 1000); //set station mode
    esp8266Data("AT+CWJAP=\"***REMOVED***\",\"***REMOVED***\"\r\n", 2000);   //connect wifi network
    long int time = millis();
    //while ((time + 5000) > millis()) {
    while (!esp8266.find("OK")) {
    }
    //}

    //esp8266Data("AT+CIFSR\r\n", 1000);
    //To-do: only for sound
    //To-do: button for scrolling
    esp8266Data("AT+CIPSTART=\"TCP\"," + ipAddr + ",3000\r\n", 2000);
    if (DEBUG) {
      Serial.println("Esp8266 connected");
    }
    initiateInternet = false;
  }
  scrollPinState = digitalRead(scrollPin);
  if ((joystickPinState == 0 || scrollPinState == 0 || mapX > 50 || mapX < -50 || mapY > 50 || mapY < -50) && !initiateInternet) {
    output = String(mapX) + "," + String(mapY) + "," + String(joystickPinState) + "," + String(scrollPinState) + "\r\n";
    sendLength = "AT+CIPSEND=" + String(output.length()) + "\r\n";
    esp8266Data(sendLength, 100);
    result = esp8266Data(output, 100);
    if (result.indexOf("Error") > 0) {
      ResetEsp8266();
    }
    if (DEBUG) {
      Serial.print(output);
    }
    digitalWrite(blueLedPin, LOW);
  }

  delay(200);
  digitalWrite(redLedPin, HIGH);
  if (!initiateInternet) {
    digitalWrite(blueLedPin, HIGH);
  }
}

void ResetEsp8266() {
  BlinkEsp8266Led();
  if (DEBUG) {
    Serial.println("Resetting esp8266");
  }
  esp8266Data("AT+RST\r\n", 2000); //reset module
  esp8266Data("AT+CWMODE=3\r\n", 1000); //set station mode
  esp8266Data("AT+CWJAP=\"***REMOVED***\",\"***REMOVED***\"\r\n", 2000);   //connect wifi network
  long int time = millis();
  //while ((time + 5000) > millis()) {
  while (!esp8266.find("OK")) {
  }
  //}

  //esp8266Data("AT+CIFSR\r\n", 1000);
  //To-do: only for sound
  //To-do: button for scrolling
  esp8266Data("AT+CIPSTART=\"TCP\"," + ipAddr + ",3000\r\n", 2000);
  if (DEBUG) {
    Serial.println("Esp8266 connected");
  }
}

void BlinkEsp8266Led() {
  digitalWrite(blueLedPin, LOW);
  delay(250);
  digitalWrite(blueLedPin, HIGH);
  delay(250);
  digitalWrite(blueLedPin, LOW);
  delay(250);
  digitalWrite(blueLedPin, HIGH);
  delay(250);
  digitalWrite(blueLedPin, LOW);
  delay(250);
  digitalWrite(blueLedPin, HIGH);
  delay(250);
  digitalWrite(blueLedPin, LOW);
  delay(250);
  digitalWrite(blueLedPin, HIGH);
}

String esp8266Data(String command, const int timeout) {
  String response = "";
  esp8266.print(command);
  long int time = millis();
  while ((time + timeout) > millis()) {
    while (esp8266.available()) {
      char c = esp8266.read();
      response += c;
    }
  }
  if (DEBUG) { //  && timeout > 999
    Serial.print(response);
  }
  return response;
}

/*
  // Rescale analog read value to potentiometer's voltage (from 0V to 5V):
  // float voltage = floatMap(analogValue, 0, 1023, 0, 5);
  float floatMap(float x, float in_min, float in_max, float out_min, float out_max) {
  return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
  }
*/
/*
  Serial.println(F("Enter number of signal to send (1 .. 20)"));
  long commandno = Serial.parseInt();
  joystickButtonPinitch (commandno) {
  case 1L:
    irsend.sendNEC(0x807F08F7U, IR_CODE_NUM_BITS);
    break;
  case 2L:
    irsend.sendNEC(0x807FE817U, IR_CODE_NUM_BITS);
    break;
  case 3L:
    irsend.sendNEC(0x807F52ADU, IR_CODE_NUM_BITS);
    break;
  case 4L:
    irsend.sendNEC(0x807F926DU, IR_CODE_NUM_BITS);
    break;
  case 5L:
    irsend.sendNEC(0x807FE21DU, IR_CODE_NUM_BITS);
    break;
  case 6L:
    irsend.sendNEC(0x807F50AFU, IR_CODE_NUM_BITS);
    break;
  case 7L:
    irsend.sendNEC(0x807F8877U, IR_CODE_NUM_BITS);
    break;
  case 8L:
    irsend.sendNEC(0x807F28D7U, IR_CODE_NUM_BITS);
    break;
  case 9L:
    irsend.sendNEC(0x807F10EFU, IR_CODE_NUM_BITS);
    break;
  case 10L:
    irsend.sendNEC(0x807F906FU, IR_CODE_NUM_BITS);
    break;
  case 11L:
    irsend.sendNEC(0x807F7A85U, IR_CODE_NUM_BITS);
    break;
  case 12L:
    irsend.sendNEC(0x807FC23DU, IR_CODE_NUM_BITS);
    break;
  case 13L:
    irsend.sendNEC(0x807F02FDU, IR_CODE_NUM_BITS);
    break;
  case 14L:
    irsend.sendNEC(0x807FC837U, IR_CODE_NUM_BITS);
    break;
  case 15L:
    irsend.sendNEC(0x807FB24DU, IR_CODE_NUM_BITS);
    break;
  case 16L:
    irsend.sendNEC(0x807F32CDU, IR_CODE_NUM_BITS);
    break;
  case 17L:
    irsend.sendNEC(0x807FD22DU, IR_CODE_NUM_BITS);
    break;
  case 18L:
    irsend.sendNEC(0x807F0AF5U, IR_CODE_NUM_BITS);
    break;
  case 19L:
    irsend.sendNEC(0x807FF20DU, IR_CODE_NUM_BITS);
    break;
  case 20L:
    irsend.sendNEC(0x807F728DU, IR_CODE_NUM_BITS);
    break;
  case 21L:
    irsend.sendSAMSUNG(POWER, IR_CODE_NUM_BITS);
    break;
  case 22L:
    irsend.sendSAMSUNG(VOLUME_UP, IR_CODE_NUM_BITS);
    break;
  case 23L:
    irsend.sendSAMSUNG(VOLUME_DOWN, IR_CODE_NUM_BITS);
    break;
  default:
    Serial.println(F("Invalid number entered, try again"));
    break;
  }

  #define POWER 0xE0E040BF
  #define VOLUME_UP 0xE0E0E01F
  #define VOLUME_DOWN 0xE0E0D02F

*/

// Command #1: Power
// Protocol: nec1, Parameters: hex=247U D=1U param3=0U param4=0U F=16U

// Command #2: Mute
// Protocol: nec1, Parameters: hex=23U D=1U param3=0U param4=0U F=23U

// Command #3: BT
// Protocol: nec1, Parameters: hex=173U D=1U param3=0U param4=0U F=74U

// Command #4: Optical
// Protocol: nec1, Parameters: hex=109U D=1U param3=0U param4=0U F=73U

// Command #5: Line In
// Protocol: nec1, Parameters: hex=29U D=1U param3=0U param4=0U F=71U

// Command #6: USB
// Protocol: nec1, Parameters: hex=175U D=1U param3=0U param4=0U F=10U

// Command #7: up arrow
// Protocol: nec1, Parameters: hex=119U D=1U param3=0U param4=0U F=17U

// Command #8: right arrow
// Protocol: nec1, Parameters: hex=215U D=1U param3=0U param4=0U F=20U

// Command #9: down arrow
// Protocol: nec1, Parameters: hex=239U D=1U param3=0U param4=0U F=8U

// Command #10: left arrow
// Protocol: nec1, Parameters: hex=111U D=1U param3=0U param4=0U F=9U

// Command #11: Select
// Protocol: nec1, Parameters: hex=133U D=1U param3=0U param4=0U F=94U

// Command #12: General
// Protocol: nec1, Parameters: hex=61U D=1U param3=0U param4=0U F=67U

// Command #13: Voice
// Protocol: nec1, Parameters: hex=253U D=1U param3=0U param4=0U F=64U

// Command #14: Reset
// Protocol: nec1, Parameters: hex=55U D=1U param3=0U param4=0U F=19U

// Command #15: Bass
// Protocol: nec1, Parameters: hex=77U D=1U param3=0U param4=0U F=77U

// Command #16: Bass+
// Protocol: nec1, Parameters: hex=205U D=1U param3=0U param4=0U F=76U

// Command #17: Bass-
// Protocol: nec1, Parameters: hex=45U D=1U param3=0U param4=0U F=75U

// Command #18: Treble
// Protocol: nec1, Parameters: hex=245U D=1U param3=0U param4=0U F=80U

// Command #19: Treble+
// Protocol: nec1, Parameters: hex=13U D=1U param3=0U param4=0U F=79U

// Command #20: Treble-
// Protocol: nec1, Parameters: hex=141U D=1U param3=0U param4=0U F=78U
