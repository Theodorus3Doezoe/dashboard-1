

namespace dashboard.Component;

public partial class Module : ContentView
{
    public Module()
    {
        InitializeComponent();
        TemperatureLabel.Text = "";
        HeartLabel.Text = "";
        ZuurstofLabel.Text = "";
    }
    

    
    public void SetHeartLabel(string heart)
    {
        HeartLabel.Text = heart;
    }
    public void SetTemperature(string temp)
    {
        TemperatureLabel.Text = temp;
    }
    
    public void SetZuurstof(string temp)
    {
        ZuurstofLabel.Text = temp;
    }

}
