# HNTAS Digital Core API (`HNTAS.DIGITAL.CORE`)

## Overview
Backend API responsible for business logic, validation, and data persistence for the HNTAS platform.

---

## ⚙️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Runtime** | .NET 9, C# |
| **API Framework** | ASP.NET Core Web API |
| **Database** | AWS DocumentDB (MongoDB-compatible) |
| **Container Hosting** | AWS ECS Fargate |
| **Storage** | AWS S3 |
| **API & Security** | AWS API Gateway, AWS WAF |
| **Monitoring & Logs** | AWS CloudWatch |
| **CI/CD** | AWS CodePipeline, GitHub |
| **Testing** | JMeter |

## Running Locally

### Prerequisites
- .NET 9 SDK
- Docker Desktop installed

### Install Docker
Download and install Docker Desktop from:  
https://www.docker.com/products/docker-desktop/

Ensure Docker is running before continuing.

---

#### Pull MongoDB Image
Pull the official MongoDB image from Docker Hub:

```bash
docker pull mongo:latest
```


#### Start MongoDB Container
If you use Docker, start a local MongoDB container:

```bash
docker run --name hntas-mongo -p 27017:27017 -d mongo:latest
```

### Run API
```bash
cd src/HNTAS.DIGITAL.CORE
dotnet run --launch-profile https
```

---

## API Endpoint
- Example: `https://localhost:7117`


