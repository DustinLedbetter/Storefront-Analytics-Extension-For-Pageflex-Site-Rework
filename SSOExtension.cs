/***************************************************************************************************************************************************
*                                                 GOD First                                                                                        *
* Authors: Core Logic Created By: Michael Lowell, Turned Into Extension By: Mike Sims, Modernized and Added to By: Dustin Ledbetter                  *
* Release Date: 12-10-2018                                                                                                                          *
* Version: 3.0                                                                                                                                     *
* Purpose: This is an extension for pageflex storefronts that creates a new option tab at the top of users who have access that allows them to     *
*          punch out to another page to see google analytics for the site.                                                                         *
***************************************************************************************************************************************************/

/*
    References: There are six dlls referenced by this template:
    First four are added references
    1. PageflexServices.dll
    2. StorefrontExtension.dll
    3. SXI.dll
    4. PFWEeb.dll
    These are added From NuGet Package Management:
    4. Microsoft.AspNet.WebApi.Client.5.2.7
    5. Newtonsoft.Json.9.0.1


NOTE: There is a inner reference call to Newtonsoft.Json.6.0.4 That throws an error if it cannot find this version. 
      This caused a clash between two extensions needing different versions of the same file. 

Solution: Patrick OMalley and Dustin Ledbetter found an assembly binding fixed this issue when placed into the web.config file of the deployment

Snippet included below:

   <runtime>
     <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
       <dependentAssembly>
         <assemblyIdentity name="Newtonsoft.Json" culture="neutral" publicKeyToken="30ad4fe6b2a6aeed" />
         <bindingRedirect oldVersion="0.0.0.0-9.0.0.0" newVersion="9.0.0.0" />
       </dependentAssembly>
     </assemblyBinding>
   </runtime>

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Net.Http;
using System.Net.Http.Headers;

using Pageflex.Interfaces.Storefront;
using PFWeb;
using PageflexServices;



namespace StorefrontAnalytics.SSO.PF
{
    public class SSOExtension : SXIExtension
    {

        #region |--Fields--|
        // This section holds variables for code used throughout the program for quick refactoring as needed

        // Used to setup the minimum required fields
        private const string _UNIQUE_NAME = @"storefrontanalytics.sso.pf";
        private const string _DISPLAY_NAME = @"Services: Storefront Analytics SSO Extension";

        // Used to setup connection to the Storefront Analyitics Page
        private const string _PATH = @"nulled for security";
        private const string _PROTOCOL = @"nulled for security";
        private const string _SA_LINK_NAME = @"SaLinkName";
        private const string _SA_LINK_NAME_TWO = @"SaLinkNameTwo";
        private const string _SA_LINK_TWO_URL = @"SaLinkTwoUrl";
        private const string _SA_ADD_LINK_TWO_FLAG = @"SaAddLinkTwoFlag";
        private const string _SA_STOREFRONT_NAME = @"SaStorefrontName";
        private const string _SA_DOMAIN = @"SaDomain";

        // Used to hold the list of characters that are not allowed to be used
        private const string _RESERVED_CHARACTERS = "nulled for security";

        // Used to setup if in debug mode and the logging path for if we are 
        private const string _SA_DEBUGGING_MODE = @"SaDebuggingMode";
        private static readonly string LOG_FILENAME1 = "D:\\Pageflex\\Deployments\\";
        private static readonly string LOG_FILENAME2 = "\\Logs\\Storefront_Analytics_Extension_Logs\\Storefront_Analytics_Extension_Log_File_";
        // Create instance for using the LogMessageToFile class methods
        LogMessageToFile LMTF = new LogMessageToFile();

        #endregion


        #region |--Properties and Logging--|
        // At a minimum your extension must override the DisplayName and UniqueName properties.


        // The UniqueName is used to associate a module with any data that it provides to Storefront.
        public override string UniqueName
        {
            get
            {
                return _UNIQUE_NAME;
            }
        }

        // The DisplayName will be shown on the Extensions and Site Options pages of the Administrator site as the name of your module.
        public override string DisplayName
        {
            get
            {
                return _DISPLAY_NAME;
            }
        }

        // Gets the parameters entered on the extension page for this extension
        protected override string[] PARAMS_WE_WANT
        {
            get
            {
                return new string[7]
                {
                  _SA_LINK_NAME,
                  _SA_LINK_NAME_TWO,
                  _SA_LINK_TWO_URL,
                  _SA_ADD_LINK_TWO_FLAG,
                  _SA_STOREFRONT_NAME,
                  _SA_DOMAIN,
                  _SA_DEBUGGING_MODE
                };
            }
        }

        // Used to access the storefront to retrieve variables
        ISINI SF { get { return Storefront; } }

        #endregion


        #region |--This section setups up the extension config page on the storefront to takes input for variables from the user at setup to be used in our extension--|

        // This section sets up on the extension page on the storefront a check box for users to turn on or off debug mode and text fields to get logon info for DB and Avalara
        public override int GetConfigurationHtml(KeyValuePair[] parameters, out string HTML_configString)
        {
            // Load and check if we already have a parameter set
            LoadModuleDataFromParams(parameters);

            // If not then we setup one 
            if (parameters == null)
            {
                SConfigHTMLBuilder sconfigHtmlBuilder = new SConfigHTMLBuilder();
                sconfigHtmlBuilder.AddHeader();

                // Add checkbox to let user turn on and off debug mode
                sconfigHtmlBuilder.AddServicesHeader("Debug Mode:", "");
                sconfigHtmlBuilder.AddCheckboxField("Debugging Information", _SA_DEBUGGING_MODE, "true", "false", (string)ModuleData[_SA_DEBUGGING_MODE] == "true");
                sconfigHtmlBuilder.AddTip(@"This box should be checked if you wish for debugging information to be output to the Storefront's Logs Page. <br> &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp 
                                            Whether this box is checked or not, the extension will log to a .txt file saved to the site's deployment folder.");
                sconfigHtmlBuilder.AddTip(@"* Make sure the 'Logs/Storefront_Analytics_Extension_Logs' folders have been created to hold the .txt files as the extension will crash without it *");

                // Add textboxes to retrieve the setup variables for setting up Storefront Analytics
                sconfigHtmlBuilder.AddServicesHeader();
                sconfigHtmlBuilder.AddTextField("Link Name", _SA_LINK_NAME, (string)ModuleData[_SA_LINK_NAME], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the name that will appear on the button link for the analytics site.");
                sconfigHtmlBuilder.AddTextField("Storefront Name", _SA_STOREFRONT_NAME, (string)ModuleData[_SA_STOREFRONT_NAME], true, true, "");
                sconfigHtmlBuilder.AddTip(@"The field should contain the name used to reference this storefront in the Storefront Analytics Web App.");
                sconfigHtmlBuilder.AddTextField("Storefront Analytics Domain", _SA_DOMAIN, (string)ModuleData[_SA_DOMAIN], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the domain of the analytics site. This will likey differ between development and production instances of the storefronts and the analytics site."); 
                sconfigHtmlBuilder.AddTip(@"The domain should be in the form 'www.somedomain.com'.");

                // This is for adding a second link to the site for a different report
                sconfigHtmlBuilder.AddServicesHeader("Optional Second Button:", "");
                sconfigHtmlBuilder.AddCheckboxField("Show Second Button on Storefront", _SA_ADD_LINK_TWO_FLAG, "true", "false", (string)ModuleData[_SA_ADD_LINK_TWO_FLAG] == "true");
                sconfigHtmlBuilder.AddTip(@"This box should be checked if you wish for the Second Button to be added to the nav bar.");
                sconfigHtmlBuilder.AddTextField("Link Two Name", _SA_LINK_NAME_TWO, (string)ModuleData[_SA_LINK_NAME_TWO], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the name that will appear on the second button.");
                sconfigHtmlBuilder.AddTextField("Link Two URL", _SA_LINK_TWO_URL, (string)ModuleData[_SA_LINK_TWO_URL], true, true, "");
                sconfigHtmlBuilder.AddTip(@"This field should contain the URL for the second button link.");

                // Footer info and set to configstring
                sconfigHtmlBuilder.AddServicesFooter();
                HTML_configString = sconfigHtmlBuilder.html;
            }
            else
            {
                SaveModuleData();
                HTML_configString = null;
            }
            return 0;
        }

        #endregion


        #region |--This section is called to get the configuration status values--|

        // Pass the module data we collected into a list
        public override int GetConfigurationStatus()
        {

            // Retrieve storefrontname to use with logging
            string storeFrontName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // Create the list to hold our configuration values from the storefront
            var configValues = new List<string>(4);

            string storefrontName = Storefront.GetValue("ModuleField", _SA_STOREFRONT_NAME, _UNIQUE_NAME);    // This variable holds the store front name the user provided from the extension setup page
            string domain = Storefront.GetValue("ModuleField", _SA_DOMAIN, _UNIQUE_NAME);                     // This variable holds the domain the user provided from the extension setup page
            string linkName = Storefront.GetValue("ModuleField", _SA_LINK_NAME, _UNIQUE_NAME);                // This variable holds the link name the user provided from the extension setup page
            string linkNameTwo = Storefront.GetValue("ModuleField", _SA_LINK_NAME_TWO, _UNIQUE_NAME);         // This variable holds the link name two the user provided from the extension setup page
            string linkTwoUrl = Storefront.GetValue("ModuleField", _SA_LINK_TWO_URL, _UNIQUE_NAME);           // This variable holds the link two url the user provided from the extension setup page

            configValues.Add(storefrontName);
            configValues.Add(domain);
            configValues.Add(linkName);
            configValues.Add(linkNameTwo);
            configValues.Add(linkTwoUrl);

            // Inform that we have called the on pageload event
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"GetConfigurationStatus Method: Storefront Name: {storefrontName}");    // Logs the storefront name
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"GetConfigurationStatus Method: Domain: {domain}");                     // Logs the domain 
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"GetConfigurationStatus Method: Link Name: {linkName}");                // Logs the link name
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"GetConfigurationStatus Method: Link Name Two: {linkNameTwo}");         // Logs the link name two
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"GetConfigurationStatus Method: Link Name Two: {linkTwoUrl}");          // Logs the link two url

            // Log the values we have currently to the storefront logs
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage($"GetConfigurationStatus Method: Storefront Name: {storefrontName}");     // Logs the storefront name
                LogMessage($"GetConfigurationStatus Method: Domain: {domain}");                      // Logs the domain 
                LogMessage($"GetConfigurationStatus Method: Link Name: {linkName}");                 // Logs the link name
                LogMessage($"GetConfigurationStatus Method: Link Name Two: {linkNameTwo}");          // Logs the link name two
                LogMessage($"GetConfigurationStatus Method: Link Two URL: {linkTwoUrl}");            // Logs the link two url
            }

            // Check to ensure we actually have values before returning them
            return configValues.Any(v => string.IsNullOrEmpty(v)) ? eDoNotCall : eSuccess;
        }

        #endregion


        #region |--This section is called on page load--|

        public override int PageLoad(string pageBaseName, string eventName)
        {

            // Setup variables to use for the user's loginname and userid
            var userID = SF.GetValue(FieldType.SystemProperty, SystemProperty.LOGGED_ON_USER_ID, null);
            var userName = SF.GetValue(FieldType.SystemProperty, SystemProperty.LOGGED_ON_USER_NAME, null);
            //var userId = Global.GetPFUserSessionObject().UserID;   // This was the older code used by Sims that is obsolete now

            // Retrieve storefrontname to use with logging
            string storeFrontName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // Check to see if we have a userid yet (if not we do nothing and exit)
            if (string.IsNullOrEmpty(userID)) return eSuccess;

            // Log messages to the file for what values we have retrieved
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"PageLoad Method: Page Base Name: " + pageBaseName + " ; Event Name: " + eventName);   
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"PageLoad Method: User ID: " + userID + "; User Name: " + userName);                 

            // Log the values we have currently to the storefront logs
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage("PageLoad Method: Page Base Name: " + pageBaseName + " ; Event Name: " + eventName);
                LogMessage("PageLoad Method: User ID: " + userID + "; User Name: " + userName);
            }


            Page page = (Page)HttpContext.Current.Handler;
            if (pageBaseName.IndexOf("user") != 0)
                return 99;
            if (eventName == "Init")
                page.Init += new EventHandler(UserPageInit);

            return eSuccess;

        }

        #endregion


        #region |--This section is called when the user's page is initialized--|

        private void UserPageInit(object sender, EventArgs e)
        {

            // Setup variables to use in this method
            Page page = (Page)HttpContext.Current.Handler;
            var storefrontUrl = string.Empty;
            var storefrontUrlTwo = string.Empty;
            var userID = SF.GetValue(FieldType.SystemProperty, SystemProperty.LOGGED_ON_USER_ID, null);

            // Retrieve storefrontname to use with logging
            string storeFrontName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);


            //
            //
            // Check if storefront analytics cookie is != null. If not then check process for previews report cookie.
            //
            //


            // Check to see if we need to add a link button or not
            if (page.Request.Cookies["StorefrontAnalytics"] != null && page.Request.Cookies["StorefrontAnalytics"]["Url"] != null)
            {
                // Log that we have added the link button to the page
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: Link Button Added");

                if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                {
                    LogMessage($"|----------------------------------------------------------|");
                    LogMessage("UserPageInit Method: Link Button Added");
                }

                storefrontUrl = page.Request.Cookies["StorefrontAnalytics"]["Url"];
                AddLinkButton(page.Controls, storefrontUrl);

                // Check if we the second link should be on the page or not
                if ((string)ModuleData[_SA_ADD_LINK_TWO_FLAG] == "true")
                {
                    if (page.Request.Cookies["PreviewsReport"] != null && page.Request.Cookies["PreviewsReport"]["Url"] != null)
                    {
                        // Log that we have added the link button to the page
                        LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                        LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: Link Button Two Added");

                        if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                        {
                            LogMessage($"|----------------------------------------------------------|");
                            LogMessage("UserPageInit Method: Link Button Two Added");
                        }

                        storefrontUrlTwo = page.Request.Cookies["PreviewsReport"]["Url"];
                        AddLinkButtonTwo(page.Controls, storefrontUrlTwo);
                        return;
                    }

                    // Get all groups associated with the retrieved userid
                    var groupsTwo = GetGroupsForUser(userID);

                    // remove unwanted chars 
                    groupsTwo = SanitizeGroupString(groupsTwo);

                    // Set groups up correctly
                    var groupStringsTwo = BreakStringIntoValidParameterLength(groupsTwo);
                    GroupResult validResultTwo = null;

                    try
                    {
                        foreach (string group in groupStringsTwo)
                        {
                            // blocking call!
                            var groupResultTwo = CheckGroupsForAuthorizationAsync(group).Result;

                            // Log that blocking call is complete
                            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: " + group + " Blocking Call Complete");

                            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                            {
                                LogMessage($"|----------------------------------------------------------|");
                                LogMessage("UserPageInit Method: " + group + " Blocking Call Complete");
                            }

                            // Check if our result is null or empty
                            if (string.IsNullOrEmpty(groupResultTwo.StorefrontUrl)) continue;

                            validResultTwo = groupResultTwo;
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
                        LogMessage($"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
                    }

                    if (validResultTwo == null || string.IsNullOrEmpty(validResultTwo.StorefrontUrl)) return;

                    page.Response.Cookies["PreviewsReport"]["Url"] = validResultTwo.StorefrontUrl;

                    // set the cookie to expire at the end of the day
                    page.Response.Cookies["PreviewsReport"].Expires = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1).AddSeconds(-1);

                    AddLinkButtonTwo(page.Controls, validResultTwo.StorefrontUrl);

                    // Log that blocking call is complete
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Adding Link Button Two Complete");

                    if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                    {
                        LogMessage($"|----------------------------------------------------------|");
                        LogMessage("UserPageInit Adding Link Button Two Complete");
                    }
                }

                return;
            }


            //
            //
            // Check if previews report cookie is != null. If not then finish process for storefront analytics cookie.
            //
            //

            // Check if we the second link should be on the page or not
            if ((string)ModuleData[_SA_ADD_LINK_TWO_FLAG] == "true")
            {
                // Check to see if we need to add a link button or not
                if (page.Request.Cookies["PreviewsReport"] != null && page.Request.Cookies["PreviewsReport"]["Url"] != null)
                {
                    // Log that we have added the link button to the page
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: Link Two Button Added");

                    if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                    {
                        LogMessage($"|----------------------------------------------------------|");
                        LogMessage("UserPageInit Method: Link Two Button Added");
                    }

                    storefrontUrl = page.Request.Cookies["PreviewsReports"]["Url"];
                    AddLinkButtonTwo(page.Controls, storefrontUrl);

                    // Get all groups associated with the retrieved userid
                    var groupsOne = GetGroupsForUser(userID);

                    // remove unwanted chars 
                    groupsOne = SanitizeGroupString(groupsOne);

                    // Set groups up correctly
                    var groupStringsOne = BreakStringIntoValidParameterLength(groupsOne);
                    GroupResult validResultOne = null;

                    try
                    {
                        foreach (string group in groupStringsOne)
                        {
                            // blocking call!
                            var groupResultOne = CheckGroupsForAuthorizationAsync(group).Result;

                            // Log that blocking call is complete
                            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: " + group + " Blocking Call Complete");

                            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                            {
                                LogMessage($"|----------------------------------------------------------|");
                                LogMessage("UserPageInit Method: " + group + " Blocking Call Complete");
                            }

                            // Check if our result is null or empty
                            if (string.IsNullOrEmpty(groupResultOne.StorefrontUrl)) continue;

                            validResultOne = groupResultOne;
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
                        LogMessage($"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
                    }

                    if (validResultOne == null || string.IsNullOrEmpty(validResultOne.StorefrontUrl)) return;

                    page.Response.Cookies["StorefrontAnalytics"]["Url"] = validResultOne.StorefrontUrl;

                    // set the cookie to expire at the end of the day
                    page.Response.Cookies["StorefrontAnalytics"].Expires = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1).AddSeconds(-1);

                    AddLinkButton(page.Controls, validResultOne.StorefrontUrl);

                    // Log that blocking call is complete
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Adding Link Button Complete");

                    if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                    {
                        LogMessage($"|----------------------------------------------------------|");
                        LogMessage("UserPageInit Adding Link Button Complete");
                    }

                    return;
                }
            }

            //
            //
            // Both cookies were null so we need to add both.
            //
            //


            // Get all groups associated with the retrieved userid
            var groups = GetGroupsForUser(userID);

            // remove unwanted chars 
            groups = SanitizeGroupString(groups);

            // Set groups up correctly
            var groupStrings = BreakStringIntoValidParameterLength(groups);
            GroupResult validResult = null;

            try
            {
                foreach (string group in groupStrings)
                {
                    // blocking call!
                    var groupResult = CheckGroupsForAuthorizationAsync(group).Result;

                    // Log that blocking call is complete
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                    LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Method: " + group + " Blocking Call Complete");

                    if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                    {
                        LogMessage($"|----------------------------------------------------------|");
                        LogMessage("UserPageInit Method: " + group + " Blocking Call Complete");
                    }

                    // Check if our result is null or empty
                    if (string.IsNullOrEmpty(groupResult.StorefrontUrl)) continue;

                    validResult = groupResult;
                    break;
                }

            }
            catch (Exception ex)
            {
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
                LogMessage($"Source: {ex.Source}\n\rMessage: {ex.Message}\n\rStack Trace: {ex.StackTrace}\n\rInner Exception:{ex.InnerException}");
            }

            if (validResult == null || string.IsNullOrEmpty(validResult.StorefrontUrl)) return;

            page.Response.Cookies["StorefrontAnalytics"]["Url"] = validResult.StorefrontUrl;

            // set the cookie to expire at the end of the day
            page.Response.Cookies["StorefrontAnalytics"].Expires = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1).AddSeconds(-1);

            AddLinkButton(page.Controls, validResult.StorefrontUrl);

            // Log that blocking call is complete
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Adding Link Button Complete");

            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage("UserPageInit Adding Link Button Complete");
            }

            if ((string)ModuleData[_SA_ADD_LINK_TWO_FLAG] == "true") 
            {
                page.Response.Cookies["PreviewsReport"]["Url"] = validResult.StorefrontUrl;

                // set the cookie to expire at the end of the day
                page.Response.Cookies["PreviewsReport"].Expires = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1).AddSeconds(-1);

                AddLinkButtonTwo(page.Controls, validResult.StorefrontUrl);

                // Log that blocking call is complete
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"UserPageInit Adding Link Button Two Complete");

                    // Check if we the second link should be on the page or not
                    if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
                {
                    LogMessage($"|----------------------------------------------------------|");
                    LogMessage("UserPageInit Adding Link Button Two Complete");
                }
            }
        }

        #endregion


        #region |-- This section is called when we need to actually add the link button--|

        public void AddLinkButton(ControlCollection cc, string storefrontUrl)
        {

            // Add code to page for the next navbar piece
            Page page = (Page)HttpContext.Current.Handler;
            string format = "<td class='navBarSeparator'></td><td class='navBarCell'><div class='navBarButton' style='float: left;'><div class='navBarButton-t'><div class='navBarButton-b'><div class='navBarButton-l'><div class='navBarButton-r'><div class='navBarButton-tl'><div class='navBarButton-tr'><div class='navBarButton-bl'><div class='navBarButton-br'><div class='navBarButton-inner'><a class='navBarButton' target='_blank' href='{0}'>{1}</a></div></div></div></div></div></div></div></div></div></div></td>";
            if (page.Request.Url.ToString().IndexOf("StorefrontAnalyticsSsoLaunch.html") != -1)
                format = format.Replace("navBarButton", "navBarButtonSelected");

            // Add the name for our navbar piece the user specified
            string text = string.Format(format, storefrontUrl, StorefrontAPI.Storefront.GetValue("ModuleField", _SA_LINK_NAME, _UNIQUE_NAME));

            // Log that the text has been set and what it is
            /*
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "AddLinkButton called text: " + text);
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") 
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage("AddLinkButton called text: " + text);
            }
            */

            //  Add navbar to correct place in list
            foreach (Control startingControl in cc)
            {
                if (startingControl.ToString().IndexOf("pageheader_ascx") != -1)
                {
                    Control childControl = ControlFinder.FindChildControl(startingControl, "phLeftSide");
                    try
                    {
                        childControl.Controls.AddAt(childControl.Controls.Count - 2, new LiteralControl(text));
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message;
                    }
                }
                if (startingControl.HasControls())
                    AddLinkButton(startingControl.Controls, storefrontUrl);
            }

        }

        #endregion


        #region |-- This section is called when we need to actually add the Second link button--|

        public void AddLinkButtonTwo(ControlCollection cc, string storefrontUrl)
        {

            // Add code to page for the next navbar piece
            Page page = (Page)HttpContext.Current.Handler;
            string format = "<td class='navBarSeparator'></td><td class='navBarCell'><div class='navBarButton' style='float: left;'><div class='navBarButton-t'><div class='navBarButton-b'><div class='navBarButton-l'><div class='navBarButton-r'><div class='navBarButton-tl'><div class='navBarButton-tr'><div class='navBarButton-bl'><div class='navBarButton-br'><div class='navBarButton-inner'><a class='navBarButton' target='_blank' href='{0}'>{1}</a></div></div></div></div></div></div></div></div></div></div></td>";
            if (page.Request.Url.ToString().IndexOf("previesreport.aspx") != -1)
                format = format.Replace("navBarButton", "navBarButtonSelected");

            // Add the name for our navbar piece the user specified
            string text = string.Format(format, storefrontUrl, StorefrontAPI.Storefront.GetValue("ModuleField", _SA_LINK_NAME_TWO, _UNIQUE_NAME));

            // Get user specfied link for link two button
            string linkTwoUrl = Storefront.GetValue("ModuleField", _SA_LINK_TWO_URL, _UNIQUE_NAME);           // This variable holds the link two url the user provided from the extension setup page

            // Log that the text has been set and what it is
            /*
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "AddLinkButton called text: " + text);
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") 
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage("AddLinkButton called text: " + text);
            }
            */

            //  Add navbar to correct place in list
            foreach (Control startingControl in cc)
            {
                if (startingControl.ToString().IndexOf("pageheader_ascx") != -1)
                {
                    Control childControl = ControlFinder.FindChildControl(startingControl, "phLeftSide");
                    try
                    {
                        childControl.Controls.AddAt(childControl.Controls.Count - 2, new LiteralControl(text));
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message;
                    }
                }
                if (startingControl.HasControls())
                    AddLinkButtonTwo(startingControl.Controls, linkTwoUrl);
            }

        }

        #endregion


        #region |--This section is called to check our groups for authorization--|

        private async Task<GroupResult> CheckGroupsForAuthorizationAsync(string groups)
        {

            // Retrieve storefrontname to use with logging
            string storeFrontName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // Log what groups we have to check
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "CheckGroupsForAuthorization begin");
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "Groups: " + groups);

            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
            {
                LogMessage($"|----------------------------------------------------------|");
                LogMessage("CheckGroupsForAuthorization begin");
                LogMessage("Groups: " + groups);
            }

            // Setup connection to test authorization
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.BaseAddress = new Uri(_PROTOCOL + StorefrontAPI.Storefront.GetValue("ModuleField", _SA_DOMAIN, _UNIQUE_NAME) + _PATH + StorefrontAPI.Storefront.GetValue("ModuleField", _SA_STOREFRONT_NAME, _UNIQUE_NAME) + "/");

            // Log the validation uri used
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "Validation URI: " + client.BaseAddress.AbsoluteUri + groups);
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "Try to add accept header");

            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true")
            {
                LogMessage("Validation URI: " + client.BaseAddress.AbsoluteUri + groups);
                LogMessage("Try to add accept header");
            }

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Log we have assigned a return data for JSON type
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "assign to return data as JSON");
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") LogMessage("assign to return data as JSON");

            var returnData = new GroupResult();

            // Log we saved to return data
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "list data response saved");
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") LogMessage("list data response saved");

            // List data response.
            HttpResponseMessage response = await client.GetAsync(groups);
            if (response.IsSuccessStatusCode)
            {
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "response is success status code");
                if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") LogMessage("response is success status code");

                returnData = await response.Content.ReadAsAsync<GroupResult>();
            }

            // Log completion of method
            LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "CheckGroupsForAuthorization end");
            if ((string)ModuleData[_SA_DEBUGGING_MODE] == "true") LogMessage("CheckGroupsForAuthorization end");

            return returnData;
        }

        #endregion


        #region |--This section is called to break a string into a section of fewer than 260 characters--|

        private List<string> BreakStringIntoValidParameterLength(string parameters)
        {
            var validStrings = new List<string>();

            validStrings.Add(string.Empty);

            // Replace all incorrect strings as blanks 
            foreach (string group in parameters.Split(','))
            {
                if (validStrings[validStrings.Count - 1].Length + group.Length > 259) validStrings.Add(string.Empty);

                validStrings[validStrings.Count - 1] += group + ",";
            }

            return validStrings;

        }

        #endregion


        // Used to get the groups that the current user has assigned to them
        private string GetGroupsForUser(string userId)
        {

            // Retrieve storefrontname to use with logging
            string storeFrontName = SF.GetValue(FieldType.SystemProperty, SystemProperty.STOREFRONT_NAME, null);

            // setup "if" condition for when the userid is null or empty
            if (string.IsNullOrEmpty(userId))
            {
                // Log when userid is empty
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, $"|----------------------------------------------------------|");
                LMTF.LogMessagesToFile(storeFrontName, LOG_FILENAME1, LOG_FILENAME2, "GetUserGroups: loggedInUser is null or empty");
                LogMessage("GetUserGroups: loggedInUser is null or empty");
                LogMessage($"|----------------------------------------------------------|");

                return string.Empty;
            }

            // Retrieve groups for user
            var userGroups = SFFieldParser.ExpandFields("<UserListProperty:AllContainingGroups>", userId, "", "");

            return userGroups;
        }


        // Used to sanitize the group string for processing
        private string SanitizeGroupString(string groups)
        {
            // remove any '/'s 
            var sanitized = groups.Replace("/", string.Empty);

            sanitized = UrlEncode(sanitized);

            return sanitized;
        }


        // Used to encode the url correctly
        private static string UrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var sb = new StringBuilder();

            foreach (char @char in value)
            {
                if (_RESERVED_CHARACTERS.IndexOf(@char) == -1)
                    sb.Append(@char);
                else
                    sb.AppendFormat("%{0:X2}", (int)@char);
            }
            return sb.ToString();
        }


        // Used to create getters and setters for authtoken and storefront url as groupresult
        public class GroupResult
        {
            public string SaAuthToken { get; set; }
            public string StorefrontUrl { get; set; }
        }


        //end of the class: SSOExtension
    }
    //end of the file
}
