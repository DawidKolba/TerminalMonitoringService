# Terminal Monitoring Service

## Overview
Terminal Monitoring Service is an advanced application designed to monitor resources of various processes simultaneously on Windows systems. It's an ideal tool for administrators and developers needing detailed insights into process resource usage.

## Key Features
- **Multi-Process Monitoring**: Monitors multiple processes concurrently, logging their CPU usage, memory usage, handle count, and process ID.
- **Dynamic Process Tracking**: Monitors all executable files in specified directories, automatically adding them to the monitoring list upon service startup.
- **Log Format for Analysis**: Logs are formatted with semicolons separating each resource metric, facilitating easy import into spreadsheet software for further analysis and charting.

## System Resource Monitoring
The service gathers detailed information about system-wide resource usage, including CPU, RAM, and disk usage metrics.

## Configuration
- Configure the service via appsettings.json to set monitoring intervals, specify processes, and designate directories for monitoring.
- The service scans the specified folders at startup and adds all .exe files to the monitoring list.

## Installation and Running
The service can be installed and run as a Windows service, ensuring uninterrupted monitoring. Use the provided InstallService.bat script for easy installation.

## Usage
Once installed, the service runs in the background, periodically checking and logging the resource usage of the monitored processes. This data is logged in a format optimized for analysis in spreadsheet software.
