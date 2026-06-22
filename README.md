# \# Heat Network Technical Assurance Scheme (HNTAS) Digital Service

# 

# \## Overview

# The HNTAS Digital Service is a unified platform designed to manage, submit, and validate heat network data in line with regulatory requirements.

# 

# \---

# 

# \## Purpose

# The service supports:

# \- Compliance with heat network regulations.

# \- Submission and validation of KPI data.

# \- Monitoring and assurance of heat network performance.

# 

# \## Architecture Overview

# The service is split into a separated frontend and backend API model running on AWS infrastructure.

# 

# \### Key Components

# \* \*\*Frontend App (`HNTAS.DIGITAL.WEB`):\*\* An ASP.NET Core MVC and Razor Pages application providing the user interface, data entry forms, and audit trail dashboards.

# \* \*\*Backend API (`HNTAS.DIGITAL.CORE`):\*\* A .NET (C#) Web API layer handling core business domain rules, validations, and persistence logic.

# \* \*\*Database:\*\* AWS DocumentDB (MongoDB-compatible) for storing KPIs and history audit trails.

# \* \*\*Hosting:\*\* AWS ECS Fargate

# \* \*\*Storage:\*\* AWS S3

# \* \*\*Authentication:\*\* GOV.UK One Login (planned/integration phase)

# \* \*\*Notifications:\*\* GOV.UK Notify

# \---

# \### High-level Architecture

# 

# \[ HNTAS.DIGITAL.WEB ] ---> \[ AWS API Gateway / WAF ] ---> \[ HNTAS.DIGITAL.CORE ]

# &#x20;  (MVC Frontend)                                              (Web API)

# &#x20;                                                                  |

# &#x20;                                                                  v

# &#x20;                                                         \[ AWS DocumentDB ]

# &#x20;                                                            (MongoDB API)

# 

# \## Technology Stack

# 

# | Layer | Technology |

# | :--- | :--- |

# | \*\*Frontend\*\* | ASP.NET Core, Razor Pages, MVC |

# | \*\*Backend\*\* | .NET, C# Web API |

# | \*\*Database\*\* | AWS DocumentDB (MongoDB-compatible) |

# | \*\*Cloud\*\* | AWS (ECS Fargate, S3, API Gateway, WAF) |

# | \*\*Monitoring\*\* | AWS CloudWatch |

# | \*\*CI/CD\*\* | AWS CodePipeline / GitHub |

# | \*\*Testing\*\* | Playwright, JMeter |

# 

# 

# \## Local Setup Instructions

# 

# \### 1. Prerequisites

# \* \*\*.NET 8.0 SDK\*\* (or your current target framework version)

# \* \*\*MongoDB Local Community Edition\*\* or Docker Desktop to host a local database instance.

# 

# \### 2. Database Initialization

# Ensure a local MongoDB instance is running on your machine on port `27017`. 

# 

# If you use Docker, run:

# ```bash

# docker run --name hntas-mongo -p 27017:27017 -d mongo:latest

# ```

# 

# \### 3. Running the System Locally

# First, clone the repository and restore all dependencies from the root directory:

# 

# ```bash

# git clone <repository-url>

# cd hntas

# dotnet restore

# ```

# 

# To run the complete digital platform, you will need to open two terminal windows to run the services side-by-side:

# 

# \### Terminal 1: Run the Backend API (HNTAS.DIGITAL.CORE)

# ```bash

# cd src/HNTAS.DIGITAL.CORE

# dotnet run --launch-profile https

# ```

# Take note of the local port address outputted to this terminal (e.g., https://localhost:7117).

# 

# 

# \### Terminal 2: Run the MVC Frontend (HNTAS.DIGITAL.WEB)

# ```bash

# cd src/HNTAS.DIGITAL.WEB

# dotnet run --launch-profile https

# ```

# 

# \### Verification

# Once both windows are running without errors:

# 

# Open your browser and navigate to the frontend address (typically https://localhost:7239).

# 

# Use the interface to submit or update metrics. The backend console will reflect database transactions and audit changes written to your local MongoDB repository.

