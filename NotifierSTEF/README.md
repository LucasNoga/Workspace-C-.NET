# HylaNOTIFY

Application en c# pour la gestion des notifications de fax

Configuration :
- Installation client RabbitMQ
- .NET Framework 4.5.2+ (SDK)
- via NuGet : installer      
    - package Tulpep - version 1.1.25
    - package RabbitMQ.Client 5.0.1
    - package Newtonsoft.Json -Version 9.0.1
    - package AutoUpdater.Net 1.4.7
    - package log4net 2.0.8
	
Log:
- Le fichier des logs se trouvent dans le repertoire C:\Users\##Login_AD##\AppData\Local\Temp\HylaNotify\Log

Configuration:
- Le fichier de configuration se trouvent dans le repertoire: C:\ProgramData\HylaNotify qui est accessible via la variable d'environnement %ALLUSERSPROFILE%