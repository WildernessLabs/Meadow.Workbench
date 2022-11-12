# Meadow.Workbench

A cross-platform management tool for Meadow devices

## Update Server

Workbench provides a locally-hosted Meadow Update Server.

## Update Binary Storage

The update server uses a local folder for storing all binary updates.  This root folder is

`%LOCALAPPDATA%\WildernessLabs\Updates`

Each available update is stored in a subfolder of this root folder based on version number (this needs to be updated to include name as well).

Inside this folder, the entire update must be contained in a single ZIP file named `update.zip`.


