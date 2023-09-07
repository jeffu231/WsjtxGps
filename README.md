### WSJTX GPS 

This is a tracker application to use the data from a Kenwood D700 or GPSD server to update the location in WSJTX for rover operations.

#### Configuration

The appsettings.json controls the behavior of the client. 

GPS Type is of gpsd for gpsd or serial for Kenwood serial interface. 

GPSSerial section is used for the serial type, specifically the Kenwood D700 serial port.

Poll: Time in seconds to poll for an update.
Baud: Baudrate of the serial port. Should match the radio setup.
Port: The serial port used. For Linux and OSX, it should be the device path. Windows the com port name.
ReadTimeout: Time in milliseconds for the port read timeout. Default to 1000.
WriteTimeout: Time in milliseconds for the port write timeout. Default to 1000.
DefaultGrid: The default maindenhead grid to supply if one cannot be obtained from the radio.

GPSD section is used when gpsd is the type.

Poll: Time in seconds to poll for an update.
Host: Hostname or IP address of the gpsd host.
Port: Port GPSD is listening on. Default is 2947.
DefaultGrid: The default maindenhead grid to supply if one cannot be obtained from the radio.


WSJTX Section

There can be one to many listeners depending on your WSJTX setup. The client supports regular and multicast options.

Ip: The IP Address of the WSJTX instance to connect to.
Port: The port to listen on. Defautls to 2237.
Multicast: If your WSJTX instance is in a multicast group and you want to join that, set Muticast to true. Ensure your Ip and port are configured for muticast groups.


