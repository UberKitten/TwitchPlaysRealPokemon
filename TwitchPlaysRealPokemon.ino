#include <Servo.h> 

int bServoPin = 10;
int aServoPin = 11;

int upPin = 7;
int downPin = 4;
int leftPin = 6;
int rightPin = 5;
int startPin = 2;
int selectPin = 3;

Servo bServo;
Servo aServo;

void setup()
{
  Serial.begin(9600);
  Serial.println("Setting up");
  bServo.attach(bServoPin);
  aServo.attach(aServoPin); 
}

int serialByte = 0;

void loop()
{
  if (Serial.available() > 0)
  {
    serialByte = Serial.read();
    
    switch (serialByte) {
      case 113: // q
        aServo.write(95);
        break;
      case 97: // a
        aServo.write(75);
        break;
      case 119: // w
        bServo.write(20);
        break;
      case 115: // s
        bServo.write(30);
        break;
      case 101: // e
        digitalWrite(upPin, HIGH);
        break;
      case 100: // d
        digitalWrite(upPin, LOW);
        break;
      case 114: // r
        digitalWrite(downPin, HIGH);
        break;
      case 102: // f
        digitalWrite(downPin, LOW);
        break;
      case 116: // t
        digitalWrite(leftPin, HIGH);
        break;
      case 103: // g
        digitalWrite(leftPin, LOW);
        break;
      case 121: // y
        digitalWrite(rightPin, HIGH);
        break;
      case 104: // h
        digitalWrite(rightPin, LOW);
        break;
      case 117: // u
        digitalWrite(startPin, HIGH);
        break;
      case 106: // j
        digitalWrite(startPin, LOW);
        break;
      case 105: // i
        digitalWrite(selectPin, HIGH);
        break;
      case 107: // k
        digitalWrite(selectPin, LOW);
        break;
      default:
        Serial.println(serialByte, DEC);
    }
  }
}
