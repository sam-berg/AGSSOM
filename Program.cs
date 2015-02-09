// Copyright 2011 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See use restrictions at /arcgis/developerkit/userestrictions.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Collections.Specialized;

namespace AGS_CommandLineSOM
{
  class Program
  {
    //Ctrl+G keyboard Bell for error messages
    const string ErrorBell = "\x7";
    const string Build = "v10.1";

    enum serviceState
    {
      Start,
      Stop,
      Restart,
      Pause
    }

    static void Main(string[] args)
    {
      AGSConnectionConfig _conn = new AGSConnectionConfig();

      try
      {
        //print usage if no arguments were passed
        if (args.Length < 1)
        {
          usage();
          return;
        }

        #region determine commands

        string sCommand = null;
        string sServer = null;
        string sUsername = "";
        string sPassword = "";
        string sInstance = "arcgis";
        string sPort = "6080";
        string sService = "";
        string sType = "";
        string sDataParam = "";
        AGSServiceConfig service = new AGSServiceConfig() { type = "MapServer", folderName = "/" };

        //look at first arg for server name
        if (args[0].IndexOf(":") < 0 && args[0].IndexOf("-") < 0)
        {
          sServer = args[0];
        }

        int iCommandIndex = -1;
        int i = 0;
        foreach (string s in args)
        {
          //find the command argument w/ "-"
          if (s.StartsWith("-"))
          {
            sCommand = s;
            iCommandIndex = i;
          }

          //find the credentials
          if (s.Split(':').Length > 1)
          {
            //username
            if (s.Split(':')[0].ToUpper() == "USER")
              sUsername = s.Split(':')[1];

           //password
            if (s.Split(':')[0].ToUpper() == "PWD")
              sPassword = s.Split(':')[1];

            //port
            if (s.Split(':')[0].ToUpper() == "PORT")
              sPort = s.Split(':')[1];

            //instance
            if (s.Split(':')[0].ToUpper() == "INSTANCE")
              sInstance = s.Split(':')[1];

            
          }
          i++;
        }

        //exit if no command was given
        if (iCommandIndex < 0)
        {
          usage();
          return;

        }

        #region param 
        //get service and folder names


        //08/2012, ensure the search for service name skips other parms with ":" (user, pwd)      
        for (int ii = iCommandIndex + 1; ii < args.Length; ii++)
        {
          string s = args[ii];
          if (s.IndexOf(":") < 0)
          {
            sService = s;

            string[] s2 = sService.Split('/');
            if (s2.Length == 1)
            {
              service.serviceName = sService;
              break;
            }
            else
            {
              service.serviceName = s2[1];
              service.folderName = s2[0];
              break;
            }
          }
        }


        int iTypeArg = (2 + iCommandIndex);

        //11.30.2010, check and see if param is type or optional param for stats, etc:
        //12.10,2010, revised to pickup optional <service> and <minutes> for stats
        if (args.Length > iTypeArg)
        {
          if (args[iTypeArg].IndexOf("Server", StringComparison.CurrentCultureIgnoreCase) > 1)
          {
            sType = args[iTypeArg++];
            service.type = sType;
          }
          else
            sDataParam = args[iTypeArg++];

          if (args.Length > iTypeArg)
            sDataParam = args[iTypeArg];
        }

        if (sType == "" && (sCommand.IndexOf("-list") < 0 || sCommand.IndexOf("-l") < 0))
        {
          sType = "MapServer";
        }

        if (sServer == null) sServer = "localhost";

        #endregion

        #endregion


        //print usage if asking for help
        if ((sCommand == null) || (!sCommand.StartsWith("-")))
        {
          Console.WriteLine(ErrorBell + "\nError: No operation specified!");
          usage2(false);
          return;
        }
        else if (sCommand == "-h")
        {
          usage2(true);
          return;
        }

        if (sCommand.ToUpper() == "-PAUSE" || sCommand.ToUpper() == "-P")
        {
          Console.WriteLine("\nThe PAUSE operation is not available.  Continuing to STOP service.");
          sCommand = "-STOP";
        }


  
        #region input params
        
        _conn.Server = sServer;
        _conn.Instance = sInstance;
        _conn.Port = sPort;
        _conn.User = sUsername;
        _conn.Password = sPassword;

        _conn.Token = generateToken(_conn);

        if (_conn.Token == null)
        {
          Console.WriteLine(ErrorBell + "\nError: Could not connect to server.");
          Console.WriteLine("\nTips: No token generated with the username and password provided.");
          Environment.Exit(1);
          return;
        }
        

        #endregion


        switch (sCommand)
        {
          case "-start"://start
          case "-s"://start
            if (sService.ToUpper() == "*ALL*" || sService.ToUpper() == "*ALL")
              StopStartAll2(_conn, serviceState.Start);
            else
            {
              Console.WriteLine();
              if (sService == "")
              {
                Console.WriteLine(ErrorBell + "Input error: Missing required 'servicename'");
                usage();
                return;
              }
              else
              {
                if (sType == "")
                  sType = "MapServer";

                StartService2(_conn, service);
              }
            }
            break;

          case "-stop"://stop
          case "-x"://stop
            if (sService.ToUpper() == "*ALL*" || sService.ToUpper() == "*ALL")
              StopStartAll2(_conn, serviceState.Stop);
            else
            {
              Console.WriteLine();
              if (sService == "")
              {
                Console.WriteLine(ErrorBell + "Input error: Missing required 'servicename'");
                usage();
                return;
              }
              else
              {
                if (sType == "")
                  sType = "MapServer";

                StopService2(_conn,service);
              }
            }

            break;

          case "-restart"://restart
          case "-r"://restart
            if (sService.ToUpper() == "*ALL*" || sService.ToUpper() == "*ALL")
              StopStartAll2(_conn, serviceState.Restart);
            else
            {
              Console.WriteLine();
              if (sService == "")
              {
                Console.WriteLine(ErrorBell + "Input error: Missing required 'servicename'");
                usage();
                return;
              }
              else
              {
                if (sType == "")
                  sType = "MapServer";

                StopService2(_conn,service);
                StartService2(_conn, service);
              }
            }
            break;

          case "-delete"://delete
            Console.WriteLine();

            if (sService == "")
            {
              Console.WriteLine(ErrorBell + "Input error: Missing required 'servicename'");
              usage();
              return;
            }
            else
            {
              if (sType == "")
              {
                Console.WriteLine(ErrorBell + "Input error: Missing or invalid 'servicetype'");
                usage();
                return;
              }
              else
                DeleteService2(_conn,service, sDataParam != "N");
            }

            break;

          case "-list"://list configurations
            Console.WriteLine("\nService Status:\n");

            #region get configurations
            List<AGSServiceConfig> _AGSConfigs = getAllConfigurations2(_conn);
            if(_AGSConfigs==null)
              Console.WriteLine("No services could be listed.");
            #endregion

            
            int iCount = 0;

            //do
            foreach (AGSServiceConfig config in _AGSConfigs)
            {
              if ((sService == "") || (sService == config.type) || (config.serviceName.ToUpper().Contains(sService.ToUpper())))
              {
                if ((sType == "") || (config.type == sType))
                {
                  string sName = concatFolderService(config.folderName, config.serviceName);
                  string sTypeName = config.type;

                  string sStatus = config.status;

                  Console.WriteLine(sTypeName + " '"  + sName + "': " + sStatus);
                  iCount += 1;
                }
              }

            } 

            if (iCount == 0)
              Console.WriteLine("No Service candidates found.");
            else
              Console.WriteLine(string.Format("\nServices found: {0}", iCount));

            break;

          case "-listtypes":
            Console.WriteLine("\nThe Listtypes command is unavailable.");
       

            break;

     
          case "-stats":
            Console.WriteLine("\nThe stats command is unavailable.");
            //esriServerTimePeriod eTimePeriod = esriServerTimePeriod.esriSTPNone;
            //int iLength = 1;
            //int iRange = 1;

            ////true if Server Summary only!
            //bool bSummaryOnly = true;
            //statsData sStatistics = new statsData();

            //#region is the servicename a number? then it's probably the starttime option.

            //if ((sService != "") && (int.TryParse(sService, out iCount) || int.TryParse(sService.Substring(0, sService.Length - 1), out iCount)))
            //{
            //  sDataParam = sService;
            //  sService = "";
            //}

            //#endregion

            //#region prep starttime interval

            //if (sDataParam != "")
            //{
            //  if (int.TryParse(sDataParam, out iLength))
            //  {
            //    //no period specified, default to minutes
            //    iRange = 61;
            //    eTimePeriod = esriServerTimePeriod.esriSTPMinute;
            //  }
            //  else
            //  {
            //    if (int.TryParse(sDataParam.Substring(0, sDataParam.Length - 1), out iLength))
            //    {
            //      switch (sDataParam.Substring(sDataParam.Length - 1).ToLower())
            //      {
            //        case "s"://seconds
            //          iRange = 61;
            //          eTimePeriod = esriServerTimePeriod.esriSTPSecond;
            //          break;
            //        case "m"://minutes
            //          iRange = 61;
            //          eTimePeriod = esriServerTimePeriod.esriSTPMinute;

            //          break;
            //        case "h"://hours
            //          iRange = 25;
            //          eTimePeriod = esriServerTimePeriod.esriSTPHour;

            //          break;
            //        case "d"://days
            //          iRange = 31;
            //          eTimePeriod = esriServerTimePeriod.esriSTPDay;

            //          break;
            //        default:
            //          Console.WriteLine(ErrorBell + "\nInput error: Unknown 'starttime' units specified.");
            //          usage();
            //          return;
            //      }
            //    }
            //    else
            //    {
            //      Console.WriteLine(ErrorBell + "\nInput error: Invalid 'starttime' value specified.");
            //      usage();
            //      return;
            //    }
            //  }

            //  //add one to interval and see if it is in acceptable range
            //  if ((++iLength < 1) || (iLength > iRange))
            //  {
            //    Console.WriteLine(ErrorBell + "\nInput error: Invalid 'starttime' interval specified, outside acceptable range.");
            //    usage();
            //    return;
            //  }
            //}

            //#endregion

            //if (sService.ToUpper() == "*ALL*" || sService.ToUpper() == "*ALL")
            //{
            //  sService = "";
            //  sType = "";

            //  //report filtered Service details not just Server Summary
            //  bSummaryOnly = false;
            //}

            //if (sService != "" || sType != "")
            //{
            //  //report filtered Service details not just Server Summary
            //  bSummaryOnly = false;
            //}

            //pConfigs = pServerObjectAdmin.GetConfigurations();
            //pConfig = pConfigs.Next();
            //iCount = 0;

            //do
            //{
            //  if ((sService == "") || (sService == pConfig.TypeName) || (pConfig.Name.ToUpper().Contains(sService.ToUpper())))
            //  {
            //    if ((sType == "") || (pConfig.TypeName == sType))
            //    {
            //      //gather Instance details regardless of statistics reporting
            //      gatherInstanceStatistics(pConfig.Name, pConfig.TypeName, sStatistics, pServerObjectAdmin);

            //      if (!bSummaryOnly)
            //      {
            //        //gather Service statistics if not just Server Summary
            //        gatherServerStatistics(pConfig.Name, pConfig.TypeName, iLength, sStatistics, eTimePeriod, pServerObjectAdmin);
            //        printServerStatistics(pConfig.Name, pConfig.TypeName, sStatistics, false);
            //      }
            //    }
            //  }
            //  pConfig = pConfigs.Next();
            //} while (pConfig != null);


            //if (bSummaryOnly)
            //{
            //  //gather Server Summary Statistics
            //  gatherServerStatistics("", "", iLength, sStatistics, eTimePeriod, pServerObjectAdmin);
            //}

            ////print Server or Service Summary
            //printServerStatistics("", "", sStatistics, !bSummaryOnly);

            break;

          case "-describe"://describe status
            Console.WriteLine("\nService Description(s):");
            
            List <AGSServiceConfig> pServiceConfigs= getAllConfigurations2(_conn);

            iCount = 0;

            foreach (AGSServiceConfig a in pServiceConfigs)
            {
              if ((service.serviceName == "") || (service.serviceName == a.type) || (a.serviceName.ToUpper().Contains(service.serviceName.ToUpper())))
              {
                if ((sType == "") || (a.type.ToUpper() == sType.ToUpper()))
                {
                  
                  describeService2(_conn,service);

                  iCount += 1;
                }
              }
            } 

            if (iCount == 0)
              Console.WriteLine("\nNo Service candidates found.");
            else
              Console.WriteLine(string.Format("\nServices found: {0}", iCount));

            break;

          default:
            Console.WriteLine(ErrorBell + string.Format("\nInput error: Unknown operation '{0}'", sCommand));
            usage();
            return;
        }
      }
      catch (Exception ex)
      {

         Console.WriteLine(ErrorBell + "\nError: " + ex.Message);
        Environment.Exit(1);
        return;
      }

      return;
    }

    static string getServiceStatus(AGSConnectionConfig conn, AGSServiceConfig service)
    {
      try
      {
 
        string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}/status", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type);
        
        string sStatus = null;

        using (WebClient _wc = new WebClient())
        {
          _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
          _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
          _wc.QueryString["f"] = "json";
          _wc.QueryString["token"] = conn.Token;

          string sResults = _wc.UploadString(sServiceURI, "");

          byte[] byteArray = Encoding.ASCII.GetBytes(sResults);
          MemoryStream m = new MemoryStream(byteArray);

          System.IO.Stream strm = m as System.IO.Stream;
          System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AGSServiceStatusConfig));

          AGSServiceStatusConfig statusobject = (AGSServiceStatusConfig)ser.ReadObject(strm);
          sStatus=statusobject.realTimeState;

        }
        service.status = sStatus;
        return sStatus;
      }
      catch (Exception ex)
      {
        return null;
      }
    }


    static string generateToken(AGSConnectionConfig conn)
    {
      try
      {
        string sTokenURI = string.Format("http://{0}:{1}/{2}/admin/generateToken", conn.Server, conn.Port,conn.Instance);

        string sToken = null;

        using (WebClient _wc = new WebClient())
        {


          //SB 2/2015
          
          _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
          _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
          //_wc.QueryString["username"] = conn.User;
          //_wc.QueryString["password"] = conn.Password;
          //_wc.QueryString["client"] = "requestip";

     

         // byte[]b = _wc.UploadValues(sTokenURI, "POST", formData);

          string sData = "username=" + conn.User + " &password=" + conn.Password + " &client=requestip";
          sToken = _wc.UploadString(sTokenURI, "POST", sData);
          
          return sToken;
          //sToken = _wc.UploadString(sTokenURI,"");

          //_wc.Headers["username"] = conn.User;
          //_wc.Headers["password"] = conn.Password;
         
        }
        if (sToken.IndexOf("<html>") < 0)
        {
          return sToken;
        }
        else
        {
          return null;
        }


      }
      catch (Exception ex)
      {
        return null;
      }
    }

    static List<AGSServiceConfig> getAllConfigurations2(AGSConnectionConfig conn)
    {
      try
      {
        string sCatalogURI = string.Format("http://{0}:{1}/{2}/admin/services/", conn.Server, conn.Port, conn.Instance);
        List<AGSServiceConfig> _list = new List<AGSServiceConfig>();
        using (WebClient _wc = new WebClient())
        {

          _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
          _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
          _wc.QueryString["f"] = "json";
          _wc.QueryString["token"] = conn.Token;

          string sResults = _wc.UploadString(sCatalogURI, "");

          byte[] byteArray = Encoding.ASCII.GetBytes(sResults);
          MemoryStream m = new MemoryStream(byteArray);

          System.IO.Stream strm = m as System.IO.Stream;
          System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AGSServiceCatalogConfig));

          AGSServiceCatalogConfig catalog = (AGSServiceCatalogConfig)ser.ReadObject(strm);
          
          foreach (AGSServiceConfig s in catalog.services)
          {
            getServiceStatus(conn, s);
            _list.Add(s);
          }


          #region subfolders
          foreach (string sFolder in catalog.folders)
          {
            sResults = _wc.UploadString(sCatalogURI + sFolder, "");

            byteArray = Encoding.ASCII.GetBytes(sResults);
            m = new MemoryStream(byteArray);

            strm = m as System.IO.Stream;
            ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AGSServiceCatalogConfig));

            catalog = (AGSServiceCatalogConfig)ser.ReadObject(strm);

            foreach (AGSServiceConfig s in catalog.services)
            {

              //AGSServiceConfig serverobject = new AGSServiceConfig();
              //serverobject.serviceName = s.serviceName;
              //serverobject.description = s.description;
              //serverobject.type = s.type;
              //serverobject.folderName = "/" + s.folderName + "/";
              getServiceStatus(conn, s);
              _list.Add(s);
            }


          }

          #endregion


          return _list;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.Message);
        return null;
      }


    }

    static AGSServiceConfig getConfiguration(AGSConnectionConfig conn, AGSServiceConfig service)
    {
      try
      {
        string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type);
        List<AGSServiceConfig> _list = new List<AGSServiceConfig>();
        using (WebClient _wc = new WebClient())
        {

          _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
          _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
          _wc.QueryString["f"] = "json";
          _wc.QueryString["token"] = conn.Token;

          string sResults = _wc.UploadString(sServiceURI, "");

          byte[] byteArray = Encoding.ASCII.GetBytes(sResults);
          MemoryStream m = new MemoryStream(byteArray);

          System.IO.Stream strm = m as System.IO.Stream;
          System.Runtime.Serialization.Json.DataContractJsonSerializer ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AGSServiceConfig));

          AGSServiceConfig s = (AGSServiceConfig)ser.ReadObject(strm);
          getServiceStatus(conn, service);
          return s;

        }
      }
      catch (Exception ex)
      {
        return null;
      }


    }

    static void describeService2(AGSConnectionConfig conn,AGSServiceConfig service)
    {
      AGSServiceConfig s = getConfiguration(conn, service);

      Console.WriteLine("\nService Name: '" + s.serviceName + "'");
      Console.WriteLine("   Type: " + s.type);
      Console.WriteLine("   Status: " + service.status);

      Console.WriteLine("   Description: " + s.description);
      Console.WriteLine("   Capabilities: " + s.capabilities);
      Console.WriteLine("   Cluster Name: " + s.clusterName);
      Console.WriteLine("   Capabilities: " + s.capabilities);
      Console.WriteLine("   Min Instances Per Node: " + s.minInstancesPerNode);
      Console.WriteLine("   Max Instances Per Node: " + s.maxInstancesPerNode);
      Console.WriteLine("   Instances per Container: " + s.instancesPerContainer);
      Console.WriteLine("   Max Wait Time: " + s.maxWaitTime);
      Console.WriteLine("   Max Startup Time: " + s.maxStartupTime);
      Console.WriteLine("   Max Idle Time: " + s.maxIdleTime);
      Console.WriteLine("   Max Usage Time: " + s.maxUsageTime);
      Console.WriteLine("   Load Balancing: " + s.loadBalancing);
      Console.WriteLine("   Isolation Level: " + s.isolationLevel);
      Console.WriteLine("   Configured State: " + s.configuredState);
      Console.WriteLine("   Recycle Interval: " + s.recycleInterval);
      Console.WriteLine("   Recycle Start Time: " + s.recycleStartTime);
      Console.WriteLine("   Keep Alive Interval: " + s.keepAliveInterval);
      Console.WriteLine("   IsDefault: " + s.isDefault);

      Console.WriteLine("   Extensions: " );
      foreach(AGSServiceExtension e in s.extensions)
      {
              Console.WriteLine("     typeName: " + e.typeName);
              Console.WriteLine("     capabilities: " + e.capabilities);
              Console.WriteLine("     enabled: " + e.enabled);
              Console.WriteLine("     maxUploadFileSize: " + e.maxUploadFileSize.ToString());
              Console.WriteLine("     allowedUploadFileTypes: " + e.allowedUploadFileTypes);

              Console.WriteLine("");

              //properties, todo
      }


    }


    #if (false)//stats
  {
    #region stats
    /// <summary>Custom Statistics class for Service Data collection and accumulation (summary totals)</summary>
    public class statsData
    {
      /// <summary>Contains Service Usage Time details</summary>
      public stat UsageTime = new stat();
      /// <summary>Contains Service Wait Time details</summary>
      public stat WaitTime = new stat();
      /// <summary>Contains Service Creation Time details</summary>
      public stat CreationTime = new stat();
      /// <summary>Contains Service Instance details</summary>
      public stat Instances = new stat();

      private static bool bAccummulative = false;

      /// <summary>Status of Service (Current only)</summary>
      public string Status = "";

      /// <summary>Start time of statistical data</summary>
      public DateTime StartTime;
      /// <summary>End time of statistical data</summary>
      public DateTime EndTime;

      /// <summary>Set to True if you wish to return Accummulated details rather than current details</summary>
      public bool ReturnAccummulative
      {
        get
        {
          return bAccummulative;
        }

        set
        {
          bAccummulative = value;
        }
      }

      public class stat
      {
        private int iSuccess = 0;
        private int iAccumSuccess = 0;
        private int iFailure = 0;
        private int iAccumFailure = 0;
        private int iTimeout = 0;
        private int iAccumTimeout = 0;
        private double dMin = -1;
        private double dAccumMin = -1;
        private double dMax = 0;
        private double dAccumMax = 0;
        private double dSum = 0;
        private double dAccumSum = 0;

        /// <summary>Get or Set number of Successful requests. Value is automatically added to Accummulation.</summary>
        public int Success
        {
          get
          {
            if (bAccummulative)
              return iAccumSuccess;
            else
              return iSuccess;
          }

          set
          {
            iSuccess = value;
            if (!bAccummulative) iAccumSuccess += value;
          }
        }

        /// <summary>Get or Set number of Failed requests. Value is automatically added to Accummulation.</summary>
        public int Failure
        {
          get
          {
            if (bAccummulative)
              return iAccumFailure;
            else
              return iFailure;
          }

          set
          {
            iFailure = value;
            if (!bAccummulative) iAccumFailure += value;
          }
        }

        /// <summary>Get or Set number of Timed out requests. Value is automatically added to Accummulation.</summary>
        public int Timeout
        {
          get
          {
            if (bAccummulative)
              return iAccumTimeout;
            else
              return iTimeout;
          }

          set
          {
            iTimeout = value;
            if (!bAccummulative) iAccumTimeout += value;
          }
        }

        /// <summary>Get total number of requests (Success+Failure+Timeout)</summary>
        public int Count
        {
          get
          {
            if (bAccummulative)
              return iAccumSuccess + iAccumFailure + iAccumTimeout;
            else
              return iSuccess + iFailure + iTimeout;
          }
        }

        /// <summary>Get or Set Minimum value (could be time in seconds or other). Value is automatically added to Accummulation.</summary>
        public double Minimum
        {
          get
          {
            if (bAccummulative)
            {
              if (dAccumMin < 0)
                return 0;
              else
                return dAccumMin;
            }
            else
            {
              if (dMin < 0)
                return 0;
              else
                return dMin;
            }
          }

          set
          {
            dMin = value;

            if (!bAccummulative)
            {
              if (dAccumMin < 0)
                dAccumMin = value;
              else if (value < dAccumMin)
                dAccumMin = value;
            }
          }
        }

        /// <summary>Get or Set Maximum value (could be time in seconds or other). Value is automatically added to Accummulation.</summary>
        public double Maximum
        {
          get
          {
            if (bAccummulative)
              return dAccumMax;
            else
              return dMax;
          }

          set
          {
            dMax = value;

            if (!bAccummulative && (value > dAccumMax)) dAccumMax = value;
          }
        }

        /// <summary>Get or Set Summary value (could be total time in seconds or other). Value is automatically added to Accummulation.</summary>
        public double Sum
        {
          get
          {
            if (bAccummulative)
              return dAccumSum;
            else
              return dSum;
          }

          set
          {
            dSum = value;
            if (!bAccummulative) dAccumSum += value;
          }
        }

        /// <summary>Get Average value (Sum/Success)</summary>
        public double Average
        {
          get
          {
            if (Success > 0)
              return Sum / Success;
            else
              return 0;
          }
        }
      }
    }


    /// <summary>
    /// gather Service and Instance Statistics
    /// </summary>
    /// <param name="sService">Name of Service</param>
    /// <param name="sType">Service Type</param>
    /// <param name="cStats">Current Statistics Object</param>
    /// <param name="pServerObjectAdmin">Administrative Object</param>
    static void gatherInstanceStatistics(string sService, string sType, statsData cStats, IServerObjectAdmin2 pServerObjectAdmin)
    {
      IServerObjectConfigurationStatus pStatus = (IServerObjectConfigurationStatus)pServerObjectAdmin.GetConfigurationStatus(sService, sType);
      cStats.Status = pStatus.Status.ToString().Substring(6);

      IServerObjectConfiguration2 pConfig = (IServerObjectConfiguration2)pServerObjectAdmin.GetConfiguration(sService, sType);

      cStats.Instances.Success = 1;                     //Set Service count
      cStats.Instances.Minimum = pConfig.MinInstances;  //Current Minimum limit
      cStats.Instances.Maximum = pConfig.MaxInstances;  //Current Maximum limit
      cStats.Instances.Sum = pStatus.InstanceCount;     //Current running instances
    }

    /// <summary>
    /// gather Service and Server Statistics
    /// </summary>
    /// <param name="sService">Name of Service or blank for Summary(Server or Service)</param>
    /// <param name="sType">Service Type or blank for Summary(Server or Service)</param>
    /// <param name="iLength">Number of Time Periods to collect</param>
    /// <param name="cStats">Current Statistics Object</param>
    /// <param name="eTimePeriod">Time Period type(seconds, minutes, hours, or days)</param>
    /// <param name="pServerObjectAdmin">Administrative Object</param>
    /// 
    static void gatherServerStatistics(string sService, string sType, int iLength, statsData cStats, esriServerTimePeriod eTimePeriod, IServerObjectAdmin2 pServerObjectAdmin)
    {

      IServerStatistics pServerStats = (IServerStatistics)pServerObjectAdmin;
      IServerTimeRange pStr;
      IStatisticsResults pStats;

      int iIndex = 0;

      #region Service Usage Time

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEContextReleased, eTimePeriod, iIndex, iLength, sService, sType, "");
      pStr = (IServerTimeRange)pStats;

      cStats.UsageTime.Success = pStats.Count;
      cStats.UsageTime.Minimum = pStats.Minimum;
      cStats.UsageTime.Maximum = pStats.Maximum;
      cStats.UsageTime.Sum = pStats.Sum;

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEContextUsageTimeout, eTimePeriod, iIndex, iLength, sService, sType, "");
      cStats.UsageTime.Timeout = pStats.Count;

      #endregion

      #region Service Wait Time

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEContextCreated, eTimePeriod, iIndex, iLength, sService, sType, "");

      cStats.WaitTime.Success = pStats.Count;
      cStats.WaitTime.Minimum = pStats.Minimum;
      cStats.WaitTime.Maximum = pStats.Maximum;
      cStats.WaitTime.Sum = pStats.Sum;

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEContextCreationTimeout, eTimePeriod, iIndex, iLength, sService, sType, "");
      cStats.WaitTime.Timeout = pStats.Count;

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEContextCreationFailed, eTimePeriod, iIndex, iLength, sService, sType, "");
      cStats.WaitTime.Failure = pStats.Count;

      #endregion

      #region Service Creation Time

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEServerObjectCreated, eTimePeriod, iIndex, iLength, sService, sType, "");
      cStats.CreationTime.Success = pStats.Count;
      cStats.CreationTime.Minimum = pStats.Minimum;
      cStats.CreationTime.Maximum = pStats.Maximum;
      cStats.CreationTime.Sum = pStats.Sum;

      pStats = pServerStats.GetAllStatisticsForTimeInterval(esriServerStatEvent.esriSSEServerObjectCreationFailed, eTimePeriod, iIndex, iLength, sService, sType, "");
      cStats.CreationTime.Failure = pStats.Count;

      #endregion

      if (pStr != null)
      {
        cStats.StartTime = pStr.StartTime;
        cStats.EndTime = pStr.EndTime;
      }
    }

    //global variable used to control printing of Time Range when multiple services are being listed
    static Boolean statsFlag = true;

    #endregion

    /// <summary>
    /// Print Service statistics details
    /// </summary>
    /// <param name="sService">Name of Service or blank for Summary(Server or Service)</param>
    /// <param name="sType">Service Type or blank for Summary(Server or Service)</param>
    /// <param name="cStats">Current Statistics Object</param>
    /// <param name="Summary">True if printing summary of services. False for Server summary and Service specific reporting.</param>
    static void printServerStatistics(string sService, string sType, statsData cStats, bool Summary)
    {

      if (statsFlag && (cStats.Instances.Count > 0))
      {
        Console.WriteLine("\nStatistics Time Range: ");
        Console.WriteLine("  Start Time: " + cStats.StartTime.ToString());
        Console.WriteLine("  End Time: " + cStats.EndTime.ToString());
        statsFlag = false;
      }

      if (sService == "")
      {
        // turn on Accummulative stats reporting for Service and Server Summaries
        cStats.ReturnAccummulative = true;

        if (Summary)
        {
          if (cStats.Instances.Count == 0)
          {
            Console.WriteLine("\nNo Service candidates found.");
            return;
          }
          else
          {
            if (cStats.Instances.Count == 1)
            {
              Console.WriteLine("\nTotal number of Service candidates: 1, No need for Summary...");
              return;
            }

            Console.WriteLine(string.Format("\nTotal number of Service candidates: {0}", cStats.Instances.Count));
          }

          Console.WriteLine("\nSummary:");
        }
        else
        {
          Console.WriteLine(string.Format("\nTotal number of Services: {0}", cStats.Instances.Count));

          Console.WriteLine("\nServer Summary:");
        }
      }
      else
      {
        // turn off Accummulative stats reporting for specific services
        cStats.ReturnAccummulative = false;

        Console.WriteLine("\nService Details:");
        Console.WriteLine("  Service Name: " + sService);
        Console.WriteLine("  Service Type: " + sType);
        Console.WriteLine("  Service Status: " + cStats.Status);
      }

      Console.WriteLine("\n  Service Instance Details:");
      Console.WriteLine("    Current Maximum: " + cStats.Instances.Maximum);
      Console.WriteLine("    Current Minimum: " + cStats.Instances.Minimum);
      Console.WriteLine("    Current Running: " + cStats.Instances.Sum);

      // set Accummulative stats reporting for Service Summary ONLY!
      cStats.ReturnAccummulative = Summary;

      TimeSpan reqTime = new TimeSpan(cStats.EndTime.Ticks - cStats.StartTime.Ticks);
      double nAvgReqPerSec = 0;

      if (reqTime.TotalSeconds > 0)
        nAvgReqPerSec = cStats.UsageTime.Count / reqTime.TotalSeconds;

      Console.WriteLine("\n  Service Usage Time:");
      Console.WriteLine("    Total number of requests: " + cStats.UsageTime.Count);
      Console.WriteLine("    Number of requests succeeded: " + cStats.UsageTime.Success);
      Console.WriteLine("    Number of requests timed out: " + cStats.UsageTime.Timeout);
      Console.WriteLine(string.Format("    Avg reqs / sec: {0:0.000000}", Math.Round(nAvgReqPerSec, 6)));
      Console.WriteLine(string.Format("    Avg usage time: {0:0.000000} Seconds", Math.Round(cStats.UsageTime.Average, 6)));
      Console.WriteLine(string.Format("    Min usage time: {0:0.000000} Seconds", Math.Round(cStats.UsageTime.Minimum, 6)));
      Console.WriteLine(string.Format("    Max usage time: {0:0.000000} Seconds", Math.Round(cStats.UsageTime.Maximum, 6)));
      Console.WriteLine(string.Format("    Sum usage time: {0:0.000000} Seconds", Math.Round(cStats.UsageTime.Sum, 6)));

      Console.WriteLine("\n  Service Wait Time:");
      Console.WriteLine("    Total number of requests: " + cStats.WaitTime.Count);
      Console.WriteLine("    Number of requests succeeded: " + cStats.WaitTime.Success);
      Console.WriteLine("    Number of requests failed: " + cStats.WaitTime.Failure);
      Console.WriteLine("    Number of requests timed out: " + cStats.WaitTime.Timeout);
      Console.WriteLine(string.Format("    Avg wait time: {0:0.000000} Seconds", Math.Round(cStats.WaitTime.Average, 6)));
      Console.WriteLine(string.Format("    Min wait time: {0:0.000000} Seconds", Math.Round(cStats.WaitTime.Minimum, 6)));
      Console.WriteLine(string.Format("    Max wait time: {0:0.000000} Seconds", Math.Round(cStats.WaitTime.Maximum, 6)));
      Console.WriteLine(string.Format("    Sum wait time: {0:0.000000} Seconds", Math.Round(cStats.WaitTime.Sum, 6)));

      Console.WriteLine("\n  Service Creation Time:");
      Console.WriteLine("    Total number of requests: " + cStats.CreationTime.Count);
      Console.WriteLine("    Number of requests succeeded: " + cStats.CreationTime.Success);
      Console.WriteLine("    Number of requests failed: " + cStats.CreationTime.Failure);
      Console.WriteLine(string.Format("    Avg creation time: {0:0.000000} Seconds", Math.Round(cStats.CreationTime.Average, 6)));
      Console.WriteLine(string.Format("    Min creation time: {0:0.000000} Seconds", Math.Round(cStats.CreationTime.Minimum, 6)));
      Console.WriteLine(string.Format("    Max creation time: {0:0.000000} Seconds", Math.Round(cStats.CreationTime.Maximum, 6)));
      Console.WriteLine(string.Format("    Sum creation time: {0:0.000000} Seconds", Math.Round(cStats.CreationTime.Sum, 6)));
    }
    #endif

    static void StopStartAll2(AGSConnectionConfig conn, serviceState state)
    {
      try
      {

        switch (state)
        {
          case serviceState.Start:
            Console.WriteLine("\nAttempting to start *all* stopped services:\n");
            break;
          case serviceState.Stop:
            Console.WriteLine("\nAttempting to stop *all* running services:\n");
            break;
          case serviceState.Restart:
            Console.WriteLine("\nAttempting to restart *all* running services:");
            break;
          case serviceState.Pause:
            Console.WriteLine("\nAttempting to pause *all* running services:\n");
            break;
          default:
            break;
        }

        int iCount = 0;

        List<AGSServiceConfig> _list = getAllConfigurations2(conn);


        foreach (AGSServiceConfig pConfig in _list)
        {

          switch (state)
          {
            case serviceState.Start:
              if (pConfig.status == "STOPPED" || pConfig.status == "PAUSED")
              {
                StartService2(conn, pConfig);
                iCount++;
              }
              break;
            case serviceState.Stop:
              if (pConfig.status == "STARTED" || pConfig.status == "PAUSED")
              {
                StopService2(conn, pConfig);
                iCount++;
              }
              break;
            case serviceState.Restart:
              if (pConfig.status == "STARTED" || pConfig.status == "PAUSED")
              {
                Console.WriteLine();
                StopService2(conn, pConfig);
                StartService2(conn, pConfig);
                iCount++;
              }
              break;
            case serviceState.Pause:
              if (pConfig.status == "STARTED")
              {
                Console.WriteLine("PAUSE command is not available.  Continuing to use the STOP command");
                StopService2(conn, pConfig);
                iCount++;
              }
              break;
            default:
              break;
          }

        }

        if (iCount == 0)
          Console.WriteLine("\nNo service candidates found.");
        else
          Console.WriteLine(string.Format("\nServices affected: {0}", iCount));

      }
      catch (Exception ex)
      {
        string s = ex.Message;
      }

    }

    static string  concatFolderService(string folder, string service)
    {
      //should return fol/service
      string s = "";
      if (folder != "/")
        s = folder + "/" + service;      
      else
        s = service;
      return s;
    }

    static void StartService2(AGSConnectionConfig conn, AGSServiceConfig service)
    {

      try
      {

        string sTypeName = service.type;
        string sName = concatFolderService(service.folderName, service.serviceName);

        Console.Write(string.Format("Attempting to start {0} '{1}': ", sTypeName, sName));
        service.status =  getServiceStatus(conn, service);

        if (service.status.ToUpper() == "STOPPED" || service.status.ToUpper() == "PAUSED")
        {
          string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type + "/start");
          List<AGSServiceConfig> _list = new List<AGSServiceConfig>();
          using (WebClient _wc = new WebClient())
          {

            _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
            _wc.QueryString["f"] = "json";
            _wc.QueryString["token"] = conn.Token;

            string sResults = _wc.UploadString(sServiceURI, "");

            if (getServiceStatus(conn, service) == "STARTED")
            {
              Console.WriteLine("Successfully started...");
            }
            else
            {
              Console.WriteLine("Could not be started.");
             // Environment.Exit(1);
            }

          }
        }
        else
        {
          switch (service.status)
          {
            case "DELETED":
              Console.WriteLine(string.Format("Can't be started because it was previously deleted.", sTypeName, sName));
              //Environment.Exit(1);
              break;
            case "STARTED":
              Console.WriteLine(string.Format("Is already started.", sTypeName, sName));
              //Environment.Exit(0);
              break;
            case "STARTING":
              Console.WriteLine(string.Format("Can't be started because it is already starting.", sTypeName, sName));
              //Environment.Exit(1);
              break;
            case "STOPPING":
              Console.WriteLine(string.Format("Can't be started because it is currently stopping.", sTypeName, sName));
              //Environment.Exit(1);
              break;
          }
        }

      }
      catch (Exception e)
      {
        Console.WriteLine(ErrorBell + "Error starting service.\n");
        Console.WriteLine(e.Message);
        //Environment.Exit(1);
      }

    }

    static void StopService2(AGSConnectionConfig conn, AGSServiceConfig service)
    {

      try
      {

        string sTypeName = service.type;
        string sName = concatFolderService(service.folderName, service.serviceName);

        Console.Write(string.Format("Attempting to stop {0} '{1}': ", sTypeName, sName));
        service.status = getServiceStatus(conn, service);

        if (service.status.ToUpper() == "STARTED" || service.status.ToUpper() == "PAUSED")
        {
          string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}/stop", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type );
          List<AGSServiceConfig> _list = new List<AGSServiceConfig>();
          using (WebClient _wc = new WebClient())
          {

            _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
            _wc.QueryString["f"] = "json";
            _wc.QueryString["token"] = conn.Token;

            string sResults = _wc.UploadString(sServiceURI, "");

            if (getServiceStatus(conn, service) == "STOPPED")
            {
              Console.WriteLine("Successfully stopped...");
            }
            else
            {
              Console.WriteLine("Could not be stopped.");
             // Environment.Exit(1);
            }

          }
        }
        else
        {
          switch (service.status)
          {
            case "DELETED":
              Console.WriteLine(string.Format("Can't be started because it was previously deleted.", sTypeName, sName));
             // Environment.Exit(1);
              break;
            case "STOPPED":
              Console.WriteLine(string.Format("Is already stopped.", sTypeName, sName));
              //Environment.Exit(0);
              break;
            case "STARTING":
              Console.WriteLine(string.Format("Can't be started because it is currently starting.", sTypeName, sName));
             // Environment.Exit(1);
              break;
            case "STOPPING":
              Console.WriteLine(string.Format("Can't be started because it is already stopping.", sTypeName, sName));
              //Environment.Exit(1);
              break;
          }
        }

      }
      catch (Exception e)
      {
        Console.WriteLine(ErrorBell + "Error stopping service.\n");
        Console.WriteLine(e.Message);
        //Environment.Exit(1);
      }

    }

    static void DeleteService2(AGSConnectionConfig conn, AGSServiceConfig service, bool confirm)
    {
      try
      {

        string sTypeName = service.type;
        string sName = concatFolderService(service.folderName, service.serviceName);
        getServiceStatus(conn, service);

        while (confirm)
        {
          Console.Write(string.Format("Delete '{0}' {1} service, are you sure (yes or no)? ", sName, sTypeName));
          switch (Console.ReadLine().ToLower())
          {
            case "yes":
              confirm = false;
              Console.WriteLine();
              break;
            case "no":
              Console.WriteLine("\nService deletion CANCELLED!");
              return;
          }
        }

        Console.Write(string.Format("Attempting to delete {0} '{1}': ", sTypeName, sName));

        string sPrefix = "Successfully ";

        if (service.status.ToUpper() == "STARTED" || service.status.ToUpper() == "PAUSED")
        {
          string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}/stop", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type);
          List<AGSServiceConfig> _list = new List<AGSServiceConfig>();
          using (WebClient _wc = new WebClient())
          {
            _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
            _wc.QueryString["f"] = "json";
            _wc.QueryString["token"] = conn.Token;

            string sResults = _wc.UploadString(sServiceURI, "");

            if (getServiceStatus(conn, service) == "STOPPED")
            {
              Console.Write(sPrefix + "stopped, ");
              sPrefix = "and ";
            }
            else
            {
              Console.WriteLine("Could not be stopped!");
              Environment.Exit(1);
            }
          }
        }

        if (getServiceStatus(conn, service) == "STOPPED")
        {

          string sServiceURI = string.Format("http://{0}:{1}/{2}/admin/services/{3}.{4}/delete", conn.Server, conn.Port, conn.Instance, concatFolderService(service.folderName, service.serviceName), service.type);
          
          using (WebClient _wc = new WebClient())
          {
            _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _wc.Headers[HttpRequestHeader.Accept] = "text/plain";
            _wc.QueryString["f"] = "json";
            _wc.QueryString["token"] = conn.Token;

            string sResults = _wc.UploadString(sServiceURI, "");
          }

          try
          {
            if (getServiceStatus(conn, service) == null)
            {
              //see exception for successful Exception when checking for deleted service
              Console.WriteLine(sPrefix + "deleted...");
            }
            else
            {
              Console.WriteLine("Could not be deleted!");
              Environment.Exit(1);
            }
          }

          catch (Exception e)
          {

          }
        }
        else
        {
          switch (service.status)
          {
            case "DELETED":
              Console.WriteLine(string.Format("Can't be deleted because it was previously deleted!", sTypeName, sName));
              Environment.Exit(0);
              break;
            case "STARTING":
              Console.WriteLine(string.Format("Can't be deleted because it is currently starting!", sTypeName, sName));
              Environment.Exit(1);
              break;
            case "STOPPING":
              Console.WriteLine(string.Format("Can't be deleted because it is currently stopping!", sTypeName, sName));
              Environment.Exit(1);
              break;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(ErrorBell + "Error deleting service!\n");
        Console.WriteLine(e.Message);
        Environment.Exit(1);
      }
    }

    static string GetStartupType2()
    {
      return "todo";

    }

    static string GetIsolationLevel2()
    {
      return "todo";
    }

    static void usage()
    {
      usage2(false);
    }

    static void usage2(bool help)
    {
      Console.WriteLine("\nAGSSOM " + Build + ", usage:\n");
      Console.WriteLine("AGSSOM -h\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: {-s | -start}   {[servicename [servicetype]] | *all*}\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: {-x | -stop}    {[servicename [servicetype]] | *all*}\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: {-r | -restart} {[servicename [servicetype]] | *all*}\n");
      //Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: {-p | -pause}   {[servicename [servicetype]] | *all*}\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -delete servicename servicetype [N]\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -list [likename] [servicetype]\n");
      //Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -listtypes\n");
      Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -describe [likename] [servicetype]\n");
      //Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -publish MXDpath [servicetype] [servicename]\n");
     // Console.WriteLine("AGSSOM [server] [port:6080] [instance:ArcGIS] user: pwd: -stats [[[likename] [servicetype]] | *all*] [starttime]");

      if (help)
      {
        Console.WriteLine("\nOperations:");
        Console.WriteLine("         -h          extended help");
        Console.WriteLine("         -s          start a stopped service");
        Console.WriteLine("         -x          stop a started service");
        Console.WriteLine("         -r          restart (stop then start) a started service");
        //Console.WriteLine("         -p          pause a started service");
        Console.WriteLine("         -delete     delete or remove a service. First, stop it if running");
        Console.WriteLine("         -describe   describe service details. Default: all services.");
        Console.WriteLine("                     If 'servicetype' omitted, all types will be included.");
        Console.WriteLine("         -list       list status of services. Default: all services.");
        Console.WriteLine("                     If 'servicetype' omitted, all types will be included.");
        //Console.WriteLine("         -listtypes  list supported service types");
        //Console.WriteLine("         -publish    publish a service. If 'servicename' omitted, file");
        //Console.WriteLine("                     name is used as service name. Currently, only");
        //Console.WriteLine("                     supports MapServer 'servicetype'.");
        //Console.WriteLine("         -stats      print service usage statistics. Default: server summary.");
        //Console.WriteLine("                     If 'servicetype' omitted, all types will be incuded.");
        //Console.WriteLine("                     If 'starttime' omitted, server start time will be used.");
        //Console.WriteLine("\nOptions:");
        Console.WriteLine("         server      local or remote server name. Default: localhost");
        Console.WriteLine("         servicename case sensitive service name");
        Console.WriteLine("         likename    services containing like text in service name");
        Console.WriteLine("         *all*       all services are affected. Trailing asterisk is optional");
        Console.WriteLine("         servicetype case sensitive service type: MapServer(default),");
        Console.WriteLine("                     GeocodeServer, FeatureServer, GeometryServer,");
        Console.WriteLine("                     GlobeServer, GPServer, ImageServer, GeoDataServer");
        //Console.WriteLine("         N           do not ask for confirmation");
        //Console.WriteLine("         MXDpath     path and filename of Map Document to publish as a service.");
        //Console.WriteLine("                     If file extension is omitted, MXD is used.");
        //Console.WriteLine("         starttime   as '99' or '99x'. Past time interval to start mining");
        //Console.WriteLine("                     for statistics. Optional 'x' indicates interval units:");
        //Console.WriteLine("                     'S or s' for Seconds, with range from 0 to 60,");
        //Console.WriteLine("                     'M or m' for Minutes(default), with range from 0 to 60,");
        //Console.WriteLine("                     'H or h' for Hours, with range from 0 to 24, or");
        //Console.WriteLine("                     'D or d' for Days, with range from 0 to 30");
        //Console.WriteLine("                     Tip: 0 is the current interval");
      }

      Environment.Exit(-1);
    }

  }

  //class AGSServerObjectConfig
  //{
  //  public string Name { get; set; }
  //  public string TypeName { get; set; }
  //  public string Status { get; set; }
  //  public string Description { get; set; }
  //  public string Folder { get; set; }
  //}

  class AGSConnectionConfig
  {
    public string Server { get; set; }
    public string Instance { get; set; }
    public string Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string Token { get; set; }

  }

  public class AGSServiceExtension
  {

      public string typeName{get;set;}
      public string  capabilities{get;set;}
      public string enabled{get;set;}

      public int maxUploadFileSize{get;set;}
      public string allowedUploadFileTypes{get;set;}
      //public string[] properties { get; set; }


  }


  public class AGSServiceConfig
  {
    public string folderName { get; set; }
    public string serviceName { get; set; }
    public string type { get; set; }
    public string description { get; set; }
    public string status { get; set; }
    public string capabilities { get; set; }
    public string clusterName{get;set;}
    public string minInstancesPerNode {get;set;}
    public string maxInstancesPerNode{get;set;}
    public string instancesPerContainer {get;set;}
    public string maxWaitTime{get;set;}
    public string maxStartupTime{get;set;}
    public string maxIdleTime { get; set; }
    public string maxUsageTime { get; set; }
    public string loadBalancing { get; set; }
    public string isolationLevel {get;set;}
    public string configuredState{get;set;}
    public string recycleInterval { get; set; }
    public string recycleStartTime { get; set; }
    public string keepAliveInterval {get;set;}
    public string isDefault { get; set; }
    //private todo
    //properties todo
    public AGSServiceExtension[]extensions{get;set;}
    //public string[] datasets { get; set; }


  }


  public class AGSServiceCatalogConfig
  {
    public string folderName { get; set; }
    public string description { get; set; }
    public string[] folders { get; set; }
    public AGSServiceConfig[] services { get; set; }
  }

  public class AGSServiceStatusConfig
  {
    public string configuredState { get; set; }
    public string realTimeState { get; set; }
  }

}
