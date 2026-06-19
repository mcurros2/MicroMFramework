# MicroM OIDC Implementation

## Configuration
* Applications configuration is done in the control panel and stored in the configuration database.
* The `ApplicationOption` class represents the configuration options for an application.
* An applications can have an Identity Provider Role:
  * *Disabled*: the application will use local authentication. The users will be create in the application database.
  * *Identity Provider Server*: the application will act as an Identity Provider
    * When acting as an IdP you need to configure authorized application clients
    * It will only accept PAR and private jwt authentication
    * Client applications can be hosted in hte same tenant or externally
    * It will implement SSO and SLO only.
  * *Identity Provider Client*: the APP will use an external IdP
    * Must configure a JWKS endpoint to expose it's certificates

## How authentication sessions are maintained
* The APP login endpoint will act as the SSO endpoint.
* The user session in each application, including the IdP, will be maintained as local sessions
  using the authenticator configured for the IdP Application.
* Accessing a protected resource for a client APP, will trigger this flow:
  * if already authenticated in the IdP but no local session at the client APP
    * It will create a SSO session and store it in the AplicationOidcActiveSessions entity. 
    * It will be redirected back to the client APP with a SSO token and create a local sessions linked to it.
  * if it has already a local session in the client APP it doesn't need authentication.
* The client APP logout endpoint should redirect to the IdP logout endpoint.
* The IdP logout endpoint will act as the SLO url and use the backchannel to close all active sessions.
  