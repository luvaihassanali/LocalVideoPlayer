#ifndef TCP_CONTROL_H
#define TCP_CONTROL_H

#include <utils.h>
#include <SoftwareSerial.h>

void InitializeEsp8266();
void MouseControl();
void ResetEsp8266();
void TcpDataIn(const int timeout);
String TcpDataOut(String command, const int timeout);
void SendTcpKeepAlive();

#endif