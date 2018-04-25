# DumbGateway

Is CGI too rich? DumbGateway is for you!

## What the heck is this?

DumbGateway is a dumb app listens for HTTP requests.
When it recieves a request matching pre-defined path, it runs a corresponding command.

See [config.example.xml](DumbGateway/config.example.xml) for a bit further information.

## Usage

`DumbGateway.exe path\to\config.xml`

_NOTE_ User account running the app needs permission. Googling `netsh urlacl` will help.

## Building

Appropriate version of Visual Studio will work.

## License

Currently licensed under AGPL Version 3. Please contact for other licensing options including NYSL Version 0.9982.