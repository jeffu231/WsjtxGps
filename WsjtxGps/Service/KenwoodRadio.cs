using System.Globalization;
using System.IO.Ports;
using System.Text;
using MaidenheadLib;
using WsjtxGps.Util;

namespace WsjtxGps.Service;

public class KenwoodRadio: IGpsDevice, IDisposable
{
    private readonly ILogger<KenwoodRadio> _logger;
    private readonly IConfiguration _config;
    private readonly SerialPort _serialPort;
    private Timer? _timer = null;
    private const string DatePattern = "yyyyMMddHHmmss";
    private readonly string _defaultGrid;
    
    public KenwoodRadio(ILogger<KenwoodRadio> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _serialPort = new SerialPort();
        _defaultGrid = _config.GetValue<string>("GPS:DefaultGrid")??string.Empty;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kenwood Radio GPS service starting");
        
        foreach (string name in SerialPort.GetPortNames())
        {
            _logger.LogDebug("Port: {Name}", name);
        }
        
        _serialPort.PortName = _config.GetValue<string>("GPS:Port") ?? "COM1";
        _serialPort.BaudRate = _config.GetValue("GPS:Baud", 9600);
        _serialPort.DataBits = 8;
        _serialPort.Parity = Parity.None;
        _serialPort.StopBits = StopBits.One;
        _serialPort.NewLine = "\r";
        
        // Set the read/write timeouts
        _serialPort.ReadTimeout = _config.GetValue<int>("GPS:ReadTimeout", 1000);
        _serialPort.WriteTimeout = _config.GetValue<int>("GPS:WriteTimeout", 1000);;

        _serialPort.RtsEnable = true;
        
        _serialPort.ReceivedBytesThreshold = 1;
        _serialPort.DataReceived += SerialPortOnDataReceived;

        try
        {
            _serialPort.Open();
            if (_serialPort.IsOpen)
            {
                _logger.LogInformation("Connected to {Port} at {Baud}", _serialPort.PortName, _serialPort.BaudRate);
            }
            else
            {
                _logger.LogInformation("Not connected!");
            }
            
            ValidateGpsMode();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e,"An unrecoverable error opening the serial port occured");
            return Task.FromException<Exception>(e);
        }

        var poll = _config.GetValue<int>("GPS:Poll", 30);
        _timer = new Timer(RequestLocation, null, TimeSpan.Zero, TimeSpan.FromSeconds(poll));
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kenwood Radio GPS service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    
    
    private async void RequestLocation(object? state)
    {
        try
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] result = encoding.GetBytes("GDAT" + _serialPort.NewLine);
            await _serialPort.BaseStream.WriteAsync(result, 0, result.Length);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e,"An unrecoverable error occured");
            throw;
        }
    }
    
    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        _logger.LogDebug("Data received");
        if (e.EventType != SerialData.Chars) return;
        try
        {
            var response = _serialPort.ReadLine();
            if (response.StartsWith("GDAT"))
            {
                ParseLocation(response);
                return;
            }
            
            if(response.StartsWith("GU"))
            {
                ParseGpsType(response);
                return;
            }
            
            _logger.LogError("Received unexpected response from radio: {Response}", response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error reading from port {Port}",_serialPort.PortName);
            
        }
    }

    private void ParseGpsType(string response)
    {
        var message = response.Substring(2, response.Length - 2);
        var type = Convert.ToInt32(message);
        if (type == 0)
        {
            _logger.LogInformation("GPS not enabled Set Menu APRS -> GPS UNIT -> NMEA or NMEA96");
        }
        else
        {
            _logger.LogInformation("GPS Enabled");
        }
    }

    private void ParseLocation(string response)
    {
        //GDAT 00000000,000000000,000000,000000,0000,000,------,0
        _logger.LogDebug("Serial Response: {Response}", response);
        var message = response.Substring(5, response.Length - 5);
        var data = message.Split(',');
        if (data.Length == 8 && data[7] == "1")
        {

            var coord = ParseCoordinates(data[0], data[1]);
            
            var locator = MaidenheadLocator.LatLngToLocator(coord.lat, coord.lon);

            var dateSuccess = DateTime.TryParseExact("20" + data[2] + data[3], DatePattern,
                CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime);

            _logger.LogDebug("Lat:{Lat}", coord.lat);
            _logger.LogDebug("Long:{Long}", coord.lon);
            _logger.LogDebug("Time:{Date}", dateTime);
            _logger.LogDebug("Grid: {Locator}", locator);
            
            var location = new Location()
            {
                Longitude = coord.lon,
                Latitude = coord.lat,
                Grid = locator??_defaultGrid,
                TimeStamp = dateSuccess ? dateTime : DateTime.Now
            };
            
            OnLocationUpdated(location);
        }
        else
        {
            _logger.LogDebug("GPS data invalid: using default locator");
            var latLong = MaidenheadLocator.LocatorToLatLng(_defaultGrid);
            var location = new Location()
            {
                Longitude = latLong.lon,
                Latitude = latLong.lat,
                Grid = _defaultGrid,
                TimeStamp = DateTime.Now
            };
            
            OnLocationUpdated(location);
        }
    }

    private async void ValidateGpsMode()
    {
        _logger.LogInformation("Validating GPS");
        ASCIIEncoding encoding = new ASCIIEncoding();
        byte[] result = encoding.GetBytes("GU" + _serialPort.NewLine);
        
        await _serialPort.BaseStream.WriteAsync(result, 0, result.Length);
    }

    private void OnLocationUpdated(Location location)
    {
        LocationUpdated?.Invoke(this,new LocationChangedEventArgs(location));
    }

    public event EventHandler<LocationChangedEventArgs>? LocationUpdated;
    

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private (double lat, double lon) ParseCoordinates(string latitude, string longitude)
    {
        var latDeg = Convert.ToInt32(latitude.Substring(0, 2));
        var latMin = Convert.ToInt32(latitude.Substring(2, 2));
        var latSec = Convert.ToInt32(latitude.Substring(4, 3)) * .001d * 60;
        var northSouth = Convert.ToInt32(latitude.Substring(7, 1));
        if (northSouth == 1)
        {
            latDeg *= -1;
        }
            
        var longDeg = Convert.ToInt32(longitude.Substring(0, 3));
        var longMin = Convert.ToInt32(longitude.Substring(3, 2));
        var longSec = Convert.ToInt32(longitude.Substring(5, 3)) * .001d * 60;
        var eastWest = Convert.ToInt32(longitude.Substring(8, 1));
        if (eastWest == 1)
        {
            longDeg *= -1;
        }
            
        var lat = Coordinates.ConvertToDecimal(latDeg, latMin, latSec);
        var lon = Coordinates.ConvertToDecimal(longDeg, longMin, longSec);
        
        return (lat, lon);
    }
    
}