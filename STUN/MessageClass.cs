namespace Funnyppt.Net.STUN;

public enum MessageClass {
    Request = 0b00,
    Indication = 0b01,
    Response = 0b10,
    Error = 0b11,
}
