# 1. Azure AD affiliated access

Once a user is added to the personnel list, the system will do a check to see if that user has an **Affiliate account**. This is indicated the following way:

![grey means no account](/docs/images/icons/azure-ad-2.svg)

**Grey** indicates the user does not have an account.

![yellow means invitation mail not accepted](/docs/images/icons/azure-ad-1.svg)

**Yellow** means the user has **not** accepted the invitation yet. That results in the user not being able to sign in.

![gree means user has access](/docs/images/icons/azure-ad.svg)

**Green** means the user has accepted the invitation and can sign in and use the company services.

## 1.1. Getting access

To get affiliated access an external user needs a sponsor with an Equinor ID. The sponsor becomes responsible for managing the access, not the user account. **Without a sponsor Affiliates cannot have access**.

Affiliates are managed from: [AccessIT](https://accessit.equinor.com/) > [My Responsibility](https://accessit.equinor.com/MyResponsibility)  > [My Affiliates](https://accessit.equinor.com/MyResponsibility#t=tab-9)

### Step by step guide for Sponsor

To provide access for an affiliate the following steps need to happen:

#### Registrate an affiliate

1. In AccesslT - go to "My Responsibility" - "Affiliates"
1. Click on "Register new Affiliate" and fill in details. You may add an access at the same time.
1. If an access is not added the person is only registered in in AccesslT.
1. Company field is free text, but try to reuse already registered company names.
1. If the user is already registered, you can go to the next step.

#### Request Access for the affiliate

1. On the AccesslT frontpage - Search for the access the Affiliate shall have.
1. Click "Add to cart" and "View Cart"
1. Request the access on behalf of the affiliate - type in the email address of the affiliate in the search box. **Multiple affiliates can be added to the same request.** It can take a couple minutes before a newly registered Affiliate is found in the search.
2. Fill in a comment if relevant and click "Send Request"
3. On the next page you will be informed that you become the Sponsor for this access for the Affiliate.
4. **Depending on the request the access may go through additional approval steps and must be approved, before the invitation mail is sent out.**

### Step by step guide for Affiliate

To get access the affiliate has to complete the following setup:

#### Affiliate onboarding

1. This is done by the Affiliate. See details here: http://learningcontent.statoil.com/affiliateaccess/
1. The Affiliate will receive an invitation mail from Microsoft, on behalf of Equinor. If not found check the junk folder.
1. The Affiliate must click the link in the email to start the onboarding.
1. During onboarding the Affiliate must register for Microsoft multifactor logon (2-factor).
1. Information on what to do is provided in the invitation mail and on the web pages during onboarding.

#### Access Resources

1. The Affiliate can now access the application using an application specific address, or go via https://myapps.microsoft.com to find an overview of available apps.
1. When logging on, Equinor require an additional verification step, and the Affiliate must provide a code from an app, Microsoft Authenticator, or SMS code.

## 1.2. What is Affiliated Access ?

* An Affiliate is an external user without an Equinor ID (shortname), and is not registered in SAP HR. We use this term to differentiate with consultants and other externals who are given an Equinor ID as part of their onboarding.
* It is preferred to invite using the company email address of the Affiliate, not their personal email address. Company emails are better managed than personal and will also be deactivated by the company if a person move to a different company.
* The Affiliate manages the password for their account themselves.
* Equinor requires the user to onboard and use 2-factor logon controlled by Equinor. The 2-factor solution is Microsoft Azure MFA.
* The Affiliate Access solution is based on Microsoft Azure B2B functionality.

## 1.3. Troubleshooting

* **NOTE: THE AFFILIATE MUST HAVE A PERSONAL MOBILE PHONE FOR ONBOARDING AND MULTIFACTOR AUTHENTICATION.** This means that Affiliates that travel offshore and can't bring their phone, can't logon.
* The application or web services must be available from the Internet.
