public interface IIoController
{
    void SetOutput(int channel, bool state);
    bool GetInput(int channel);
}
