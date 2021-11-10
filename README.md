# ResilientWebApi

This is a simple ASP.NET Core application with an in-process implementation of the Producer/Consumer pattern.

It uses [System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels) apis as a mean to decouple requests from their actual processing.
