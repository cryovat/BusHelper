# BusHelper

This is an alternative to the [Public Transport](https://home-assistant.io/components/sensor.gtfs/)
sensor for [Home Assistant](https://github.com/home-assistant/home-assistant). I found that the GTFS
package for my local bus company bogged the program down with a huge route database that literally took
an hour to import, and had data quirks that rendered it incompatible with the sensor.

This setup consists of two parts:

 * A .NET program making use of the [GTFS](https://github.com/OsmSharp/GTFS) library to generate a tab
   delimited file of valid departures for a single route, to a single destination, form a group of stations.
 * A shell script to act as a [Command Line](https://home-assistant.io/components/sensor.command_line/) sensor.

Usage for the .NET program:

```powershell
# Arguments are <gtfs path> <destination station id> <origin station id1> <origin station id2> ...
$ BusHelper.exe "c:\path\to\unzipped\gtfs\archive" 1234 555 666 777 888 999 > routes.txt
```

Setup in Home Assistant:

```yaml
- platform: command_line
  scan_interval: 300
  command: "~/bus/get_next.sh ~/bus/routes.txt"
  name: bus_to_work

- platform: template
  sensors:
    bus_to_work_formatted:
      friendly_name: Work
      value_template: >
        {% if states('sensor.bus_to_work') %}
          {% set bus_parts = states('sensor.bus_to_work').split('|') %}
          {% if now.strftime("%Y-%m-%d") == bus_parts[1][0:10] %}
            Route {{ bus_parts[6] }} - {{ bus_parts[1][11:16] }}
          {% else %}
            No departures today
          {% endif %}
        {% else %}
          N/A
        {% endif %}
```
