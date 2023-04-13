# AppointmentService

Creating a self signed certificate
- dotnet dev-certs https --export-path path --password password

To setup the tables of a local MS SQL Database:
- cd AppointmentService
- dotnet ef migrations add First
- dotnet ef database update
