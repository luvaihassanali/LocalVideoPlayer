#include <Arduino.h>
#include "PinDefinitionsAndMore.h"
// Defines must appear before IRremote include
#define DECODE_NEC
#define DECODE_SAMSUNG
#define DECODE_SONY
#define MARK_EXCESS_MICROS 10
#include <IRremote.hpp>

const bool DEBUG = false;

const uint32_t SAMSUNG_POWER = 0x19E60707;
const uint32_t SAMSUNG_A = 0x936C0707;
const uint32_t SAMSUNG_B = 0xEB140707;
const uint32_t SAMSUNG_C = 0xEA150707;
const uint32_t SAMSUNG_D = 0xE9160707;
const uint32_t SAMSUNG_STOP = 0xB9460707;

const uint16_t SONY_POWER = 0x815;
const uint16_t SONY_VOL_UP = 0x812;
const uint16_t SONY_VOL_DOWN = 0x813;
const uint32_t SONY_UP = 0x39D78;
const uint32_t SONY_DOWN = 0x39D79;
const uint32_t SONY_RIGHT = 0x39D7B;
const uint32_t SONY_LEFT = 0x39D7A;
const uint32_t SONY_ENTER = 0x39D7C;
const uint32_t SONY_BACK = 0x39D7D;
const uint32_t SONY_PLAY = 0x39D32;
const uint32_t SONY_PAUSE = 0x39D39;

const uint32_t SOUNDBAR_POWER = 0xFE015343;
const uint32_t TV_POWER = 0xFD020707;
const uint32_t VOL_UP = 0xCC335343;
const uint32_t VOL_DOWN = 0xC43B5343;
const uint32_t UP = 0xF20D5343;
const uint32_t DOWN = 0xEA155343;
const uint32_t RIGHT = 0xE21D5343;
const uint32_t LEFT = 0xDA255343;
const uint32_t ENTER = 0xFA055343;
const uint32_t BACK = 0xE31C5343;
const uint32_t PLAY = 0xFC035343;
const uint32_t PAUSE = 0x827D5343;
const uint32_t SOUNDBAR_INPUT = 0xF9065343; // AUDIO
const uint32_t APP_POWER = 0xD7285343;      // TV/VIDEO

// The following variables are automatically generated using IrScrutinizer 2.3.0 and Bomaker Ondine 1 Soundbar.rmdu
// http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809

typedef uint16_t microseconds_t;
typedef uint16_t frequency_t;

static inline unsigned hz2khz(frequency_t f)
{
    return f / 1000U;
}

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

void checkReceive(uint16_t aSentAddress, uint16_t aSentCommand);
void ParseIrValue(uint32_t rawData);
void sendRaw(const microseconds_t intro[], size_t lengthIntro, const microseconds_t repeat[], size_t lengthRepeat, frequency_t frequency, unsigned times);
void ChangeInputSoundBar();
void PowerSoundBar();
void PowerSoundBarLong();
void Led13Blink();
void Log(String m);
void SendSerialData(String d);

// Sent address and command placeholder for checkReceive
uint16_t sAddress = 0xFFF1;
uint8_t sCommand = 0x76;

bool opticalBluetoothSwitch = false;
bool powerPressed = false;

void setup()
{
    Serial.begin(9600);
    IrReceiver.begin(IR_RECEIVE_PIN, DISABLE_LED_FEEDBACK);
    IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK);
    Log("Ready");
}

void loop()
{
    checkReceive(sAddress & 0xFF, sCommand);
    delay(50);
}

// https://github.com/Arduino-IRremote/Arduino-IRremote/tree/master/examples/UnitTest
void checkReceive(uint16_t aSentAddress, uint16_t aSentCommand)
{
    // Wait until signal has received
    delay((RECORD_GAP_MICROS / 1000) + 1);
    if (IrReceiver.decode())
    {
        if (DEBUG)
        {
            IrReceiver.printIRResultShort(&Serial);
        }
        uint32_t rawData = IrReceiver.decodedIRData.decodedRawData;
        ParseIrValue(rawData);
        IrReceiver.resume();
    }
}

void ParseIrValue(uint32_t rawData)
{
    switch (rawData)
    {
    case SAMSUNG_POWER:
    case TV_POWER:
        Log("Tv power signal received");
        PowerSoundBarLong();
        break;
    case SAMSUNG_A:
    case SOUNDBAR_POWER:
        Log("A");
        PowerSoundBar();
        break;
    case SAMSUNG_B:
    case SONY_VOL_UP:
    case VOL_UP:
        Log("B");
        sendRaw(intro_up_arrow, 68U, repeat_up_arrow, 4U, 38400U, 1);
        delay(50);
        break;
    case SAMSUNG_C:
    case SONY_VOL_DOWN:
    case VOL_DOWN:
        Log("C");
        sendRaw(intro_down_arrow, 68U, repeat_down_arrow, 4U, 38400U, 1);
        delay(50);
        break;
    case SAMSUNG_D:
    case SOUNDBAR_INPUT:
        Log("D");
        ChangeInputSoundBar();
        break;
    case SAMSUNG_STOP:
        SendSerialData("stop");
        break;
    case SONY_POWER:
    case APP_POWER:
        SendSerialData("power");
        break;
    case SONY_UP:
    case UP:
        SendSerialData("up");
        break;
    case SONY_DOWN:
    case DOWN:
        SendSerialData("down");
        break;
    case SONY_RIGHT:
    case RIGHT:
        SendSerialData("right");
        break;
    case SONY_LEFT:
    case LEFT:
        SendSerialData("left");
        break;
    case SONY_ENTER:
    case ENTER:
        SendSerialData("enter");
        break;
    case SONY_BACK:
    case BACK:
        SendSerialData("back");
        break;
    case SONY_PLAY:
    case PLAY:
        SendSerialData("play");
        break;
    case SONY_PAUSE:
    case PAUSE:
        SendSerialData("pause");
        break;
    default:
        break;
    }
}

void SendSerialData(String msg)
{
    // Transfer string to serial USB connection
    Serial.println(msg);
    // Wait for the transmission of outgoing serial data to complete
    Serial.flush();
    Led13Blink();
}

// https://github.com/Arduino-IRremote/Arduino-IRremote/tree/master/examples/SendRawDemo
void sendRaw(const microseconds_t intro[], size_t lengthIntro, const microseconds_t repeat[], size_t lengthRepeat, frequency_t frequency, unsigned times)
{
    if (lengthIntro > 0U)
    {
        IrSender.sendRaw_P(intro, lengthIntro, hz2khz(frequency));
    }
    if (lengthRepeat > 0U)
    {
        for (unsigned i = 0U; i < times - (lengthIntro > 0U); i++)
        {
            IrSender.sendRaw_P(repeat, lengthRepeat, hz2khz(frequency));
        }
    }
}

void ChangeInputSoundBar()
{
    if (opticalBluetoothSwitch)
    {
        Log("Optical");
        sendRaw(intro_BT, 68U, repeat_BT, 4U, 38400U, 1);
        opticalBluetoothSwitch = false;
    }
    else
    {
        Log("Bluetooth");
        sendRaw(intro_Optical, 68U, repeat_Optical, 4U, 38400U, 1);
        opticalBluetoothSwitch = true;
    }
    delay(50);
}

void PowerSoundBar()
{
    if (powerPressed)
    {
        Log("Power off");
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
        powerPressed = false;
    }
    else
    {
        Log("Power on");
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
        powerPressed = true;
    }
    delay(50);
}

void PowerSoundBarLong()
{
    if (powerPressed)
    {
        Log("Power off");
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
        delay(50);
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
        powerPressed = false;
    }
    else
    {
        Log("Power on");
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
        delay(50);
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
        delay(50);
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
        delay(50);
        sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
        powerPressed = true;
    }
    delay(50);
}

void Led13Blink()
{
    digitalWrite(LED_BUILTIN, HIGH);
    delay(50);
    digitalWrite(LED_BUILTIN, LOW);
}

void Log(String msg)
{
    if (DEBUG)
    {
        Serial.println(msg);
    }
}