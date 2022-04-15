#include <utils.h>

/*
  + Red Button: Input (TV <-> Audio)
  + Blue Button: Input (LVP <-> Audio)
  +-----------+----------------------------------+---------------+----------------------------------------+
  |  Control  |        1 - TV                    |    2 - LVP    |     3 - Audio                          |
  +-----------+----------------------------------+---------------+----------------------------------------+
  | Button 1  | TV Power                         | Reset         | Audio Power                            |
  | Button 2  | TV Back                          |               | Audio Input                            |
  | Button 3  | TV Home                          | Scroll        | Mute                                   |
  | J. Button | TV Enter                         | LVP Enter     | Audio Power                            |
  | J. Stick  | TV Direction (TV Vol [+Button3]) | LVP Direction | Up(+)/down(-) vol                      |
  +-----------+----------------------------------+---------------+----------------------------------------+
  + Button 3 + J.Button = TV Sound Script
  + Button 3 + J.Stick = TV Volume +/-
*/

void ControlHandler();
void InitializeEsp8266();
void InnerLoop();
void MouseControl();
void RemoteState();
void ResetEsp8266();
void SoundBarControl();
void TcpDataIn(const int timeout);
String TcpDataOut(String command, const int timeout);
void SendTcpKeepAlive();
void TvControl();

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

void setup()
{
    Serial.begin(9600);
    pinMode(JS_RX_PIN, INPUT);
    pinMode(JS_RY_PIN, INPUT);
    pinMode(JS_BTN_PIN, INPUT_PULLUP);
    pinMode(BTN1_PIN, INPUT_PULLUP);
    pinMode(BTN2_PIN, INPUT_PULLUP);
    pinMode(BTN3_PIN, INPUT_PULLUP);
    pinMode(RED_BTN_PIN, INPUT_PULLUP);
    pinMode(BLUE_BTN_PIN, INPUT_PULLUP);
    pinMode(RED_LED_PIN, OUTPUT);
    pinMode(GREEN_LED_PIN, OUTPUT);
    pinMode(BLUE_LED_PIN, OUTPUT);
    BlueLedOn();
    GreenLedOn();
    RedLedOn();
    Log("Ready");
}

void loop()
{
    currentMillis = millis();
    // Separate main loop to implement simple timer
    while ((currentMillis + KA_TIMEOUT) > millis())
    {
        InnerLoop();
    }
    // If TCP client has connected, send keep alive every 5s
    if (clientConnected)
    {
        SendTcpKeepAlive();
    }
}

void InnerLoop()
{
    button1State = digitalRead(BTN1_PIN);
    button2State = digitalRead(BTN2_PIN);
    button3State = digitalRead(BTN3_PIN);
    redButtonState = digitalRead(RED_BTN_PIN);
    blueButtonState = digitalRead(BLUE_BTN_PIN);
    joystickXPos = analogRead(JS_RX_PIN);
    joystickYPos = analogRead(JS_RY_PIN);
    joystickButtonState = digitalRead(JS_BTN_PIN);
    joystickMapX = map(joystickXPos, 0, 1023, -512, 512);
    joystickMapY = map(joystickYPos, 0, 1023, -512, 512);
    joystickOutput = String(joystickMapX) + "," + String(joystickMapY) + "," + String(joystickButtonState) + "," + String(button3State) + "\r\n";

    RemoteState();
    ControlHandler();

    // Check for incoming data if esp8266 is active
    if (esp8266Init)
    {
        TcpDataIn(50);
    }
    else
    {
        delay(50);
    }
}

void RemoteState()
{
    if (redButtonState == 0)
    {
        Log("RED_BTN");
        if (currentState == 0)
        {
            currentState = 2;
            GreenLedOn();
        }
        else if (currentState == 2)
        {
            currentState = 0;
            RedLedOn();
        }
        else if (currentState == 1)
        {
            currentState = 0;
            RedLedOn();
        }
    }

    if (blueButtonState == 0)
    {
        Log("BLUE_BTN");
        if (currentState == 0)
        {
            currentState = 1;
            BlueLedOn();
            InitializeEsp8266();
        }
        else if (currentState == 2)
        {
            currentState = 1;
            BlueLedOn();
        }
        else if (currentState == 1)
        {
            currentState = 2;
            GreenLedOn();
        }
    }
}

void ControlHandler()
{
    // If esp8266 not initialized joystick control sends infared tv remote signals
    if (currentState == 0 && (button1State == LOW || button2State == LOW || button3State == LOW  || joystickButtonState == LOW ||
                              (button3State == HIGH && joystickMapX > JS_THRESHOLD) || (button3State == HIGH && joystickMapX < -JS_THRESHOLD) || joystickMapY > JS_THRESHOLD || joystickMapY < -JS_THRESHOLD))
    {
        TvControl();
    }

    // Reset esp8266
    if (currentState == 1 && button1State == LOW) {

        if (button1State == LOW) {
            Log("Reset esp8266");
            ResetEsp8266();
        }
    }
    // Else joystick data is sent over tcp to control mouse movement
    if (clientConnected && currentState == 1 && (button1State == LOW || button3State == LOW  || joystickButtonState == LOW || joystickMapX > JS_THRESHOLD || joystickMapX < -JS_THRESHOLD || joystickMapY > JS_THRESHOLD || joystickMapY < -JS_THRESHOLD))
    {
        MouseControl();
    }
    

    // Else joystick data is sent over tcp to control mouse movement
    if (currentState == 2 && (button1State == LOW || button2State == LOW || button3State == LOW || joystickButtonState == LOW || joystickMapY > JS_THRESHOLD || joystickMapY < -JS_THRESHOLD))
    {
        SoundBarControl();
    }
}
