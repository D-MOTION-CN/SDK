namespace Connect;

[Flags]
public enum ErrorCodes
{
    DriverError          = 0x00000001,
    DriverOffline        = 0x00000002,
    OverPositionError    = 0x00000004,
    OverStrokeLimit      = 0x00000008,
    FileReadError        = 0x00000010,
    TfCardLost           = 0x00000020,
    SystemParametersLost = 0x00000040,
    LicenseInvalid       = 0x00000080,
    JsonFileParasError   = 0x00000100,
    JsonParasTypeError   = 0x00000200,
    JsonParasOverRange   = 0x00000400,
    OverMotorSpeed       = 0x00001000,
    EnableMotorFailed    = 0x00002000,
    MatrixInverseError   = 0x00004000,
}