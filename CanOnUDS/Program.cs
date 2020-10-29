using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using vxlapi_NET;
using System.Threading;
using System.Linq;
using System.Timers;
using System.Windows;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;



namespace UDSonCAN
{
    
    public class Program
    {
        /**********************Global Variables*********************************/
        //Timers::
        public System.Timers.Timer P2TIMER;
        public System.Timers.Timer TPRIMER;
        //Driver::
        private static XLDriver UDSDemo = new XLDriver();
        private static string appName = "UDS Client";
        //Driver configuration::
        private static XLClass.xl_driver_config driverConfig = new XLClass.xl_driver_config();
        private static XLClass.xl_ethernet_bus_params bus_Params = new XLClass.xl_ethernet_bus_params();
        XLDefine.XL_Status txStatus;
        XLClass.xl_event_collection xlEventCollection = new XLClass.xl_event_collection(1);
        XLClass.xl_event receivedEvent = new XLClass.xl_event();
        XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;
        // Variables required by XLDriver
        private static XLDefine.XL_HardwareType hwType = XLDefine.XL_HardwareType.XL_HWTYPE_NONE;
        private static uint hwIndex = 0;
        private static uint hwChannel = 0;
        private static int portHandle = -1;
        private static UInt64 accessMask = 0;
        private static UInt64 permissionMask = 0;
        private static UInt64 txMask = 0;
        private static UInt64 rxMask = 0;
        private static int txCi = -1;
        private static int rxCi = -1;
        private static EventWaitHandle xlEvWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);
        // RX thread
        public Thread rxThread;
        private static bool blockRxThread = false;
        public Thread LogThread;
        public Thread txThread;
        XLDefine.XL_Status status;
        public int ThreadMutex = 0;
        public string TRACE_DATA;
        public string DEFAULT_SESSION;
        public string READ_DATA_BY_ID;
        public string TESTER_PRESENT;
        //private static uint respid;
        //private static uint reqid;
        //private static uint resaddr;
        //private static uint reqaddr;
        public int FILLER = 0X55;
        public uint REQ_ID = 0X735;
        public uint RES_ID = 0X73D;
        public uint RTrigger = 0;
        public int DIDLOW;
        public int DIDHIGH;
        public int DTCLOW;
        public int DTCHIGH;
        public int DTC;
        delegate void Rxdelegate(string methodname);
        public const int P2 = 5000;
        public const int P2Ext = 5000;
        public const int S3Client = 2000;
        public const int S3Server = 2000;
        public uint TimerRate = 0;
        public Boolean response = false;
        
        //int TPLOCK = 0;

        /******************************End of Global variables***************/


        static void Main(string[] args)
        {
            Console.WriteLine("Hello to App UDS on CAN!!");
            Console.WriteLine("\n");
            var test = new Program();
            Boolean res;     
            //////////////////////////////////////////////////////
            res = test.TestCase1(test);
            Thread.Sleep(1000);
            if (res == true)
            {
                Console.WriteLine("TEST PASSED!!");
            }
            else Console.WriteLine("TEST FAILED");
            //////////////////////////////////////////////////////
            Thread.Sleep(1000);
            res = test.TestCase2(test);
            Thread.Sleep(1000);
            if (res == true)
            {
                Console.WriteLine("TEST PASSED!!");
            }
            else Console.WriteLine("TEST FAILED");
            //////////////////////////////////////////////////////
        }








        
        /////////////// Implementation of essential fucntions ////////////////
        
       
        public Boolean TestCase1(Program t1)
        {
            Boolean result = false;            
            t1.INITLOG();
            Thread.Sleep(3000);
            //Console.Clear();
            t1.TXHANDLER(0X10);
            Thread.Sleep(1000);
            if (response == true)
            {
                Console.WriteLine("Communication with CAN is successfull!! ");
                result = true;
            }else 
            {
                Console.WriteLine("Some trouble in communication with CAN !!");
                result = false;
            }
            
            return result;
        }

        public Boolean TestCase2(Program t1)
        {
            Boolean result = false;
            //t1.INITLOG();
            //Thread.Sleep(3000);
            Console.Clear();
            t1.TXHANDLER(0X11);
            Thread.Sleep(1000);
            if (response == true)
            {
                Console.WriteLine("Communication with CAN is successfull!! ");
                result = true;
            }
            else
            {
                Console.WriteLine("Some trouble in communication with CAN !!");
                result = false;
            }

            return result;
        }

        public void PrintFunctionError()
        {
            Console.WriteLine("App is crashed!!");
        }

        public void INITLOG()
        {

            //starting app
            Console.WriteLine("UDS- Vector Client Started \n");
            Console.WriteLine("Vector XL Driver Version: " + typeof(XLDriver).Assembly.GetName().Version + "\n");
            //opening driver
            status = UDSDemo.XL_OpenDriver();
            Console.WriteLine("Opening vector CAN Driver.... \n");
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            status = UDSDemo.XL_GetDriverConfig(ref driverConfig);
            //getting config
            Console.WriteLine("Getting CAN Driver Config: \n");
            Console.WriteLine(status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //getting DLL info
            Console.WriteLine("Getting Vector DLL Version: ");
            Console.WriteLine(UDSDemo.VersionToString(driverConfig.dllVersion) + Environment.NewLine);
            //Getting channels...
            Console.WriteLine("Channels found: " + driverConfig.channelCount + Environment.NewLine);
            for (int i = 0; i < driverConfig.channelCount; i++)
            {
                Console.WriteLine("   Channel Name:" + driverConfig.channel[i].name );
                Console.WriteLine("   Channel Mask:" + driverConfig.channel[i].channelMask );
                Console.WriteLine("   Transceiver Name:" + driverConfig.channel[i].transceiverName );
                Console.WriteLine("   Serial Number:" + driverConfig.channel[i].serialNumber);
                Console.WriteLine("\n\n");
            }

            //Check config
            if ((UDSDemo.XL_GetApplConfig(appName, 0, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS) ||
          (UDSDemo.XL_GetApplConfig(appName, 1, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS))
            {
                //...create the item with two CAN channels
                UDSDemo.XL_SetApplConfig(appName, 0, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                UDSDemo.XL_SetApplConfig(appName, 1, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                //PrintAssignErrorAndPopupHwConf();
                ThreadMutex = 1;
            }
            // Request the user to assign channels until both CAN1 (Tx) and CAN2 (Rx) are assigned to usable channels
            if (!GetAppChannelAndTestIsOk(0, ref txMask, ref txCi) || !GetAppChannelAndTestIsOk(1, ref rxMask, ref rxCi))
            {
                ThreadMutex = 0;
            }
            //Printing application configuration on log screen
            //PrintConfig();
            //making masks
            accessMask = txMask | rxMask;
            permissionMask = accessMask;
            //opening port
            status = UDSDemo.XL_OpenPort(ref portHandle, appName, accessMask, ref permissionMask, 1024, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            Console.WriteLine("Open Port  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //chip state checking
            status = UDSDemo.XL_CanRequestChipState(portHandle, accessMask);
            Console.WriteLine("CAN Request Chip state  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //ON Bus
            status = UDSDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
            Console.WriteLine("Activate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //can ids display
            Console.WriteLine("TESTER REQUEST CAN ID: 0x735" + Environment.NewLine);
            Console.WriteLine("ECU RESPONSE CAN ID: 0x73D" + Environment.NewLine);
            //giving info
            
            //putting notifications on can
            int tempInt = -1;
            status = UDSDemo.XL_SetNotification(portHandle, ref tempInt, 1);
            xlEvWaitHandle.SafeWaitHandle = new SafeWaitHandle(new IntPtr(tempInt), true);
            Console.WriteLine("Set Notification  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();

            if (TimerRate == 1) TimerRate = 0;
            else TimerRate = 20000;
            status = UDSDemo.XL_SetTimerRate(portHandle, TimerRate);
            Console.WriteLine( "setTimer  :" + status + Environment.NewLine);
            //resetting clock
            status = UDSDemo.XL_ResetClock(portHandle);
            Console.WriteLine("Reset Clock  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            //TPLOCK = 1;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nDevice Connected " + driverConfig.channel[0].name + " and " + driverConfig.channel[1].name + " active\n");
            Console.ForegroundColor = ConsoleColor.White;
            //starting rx thread
            Console.WriteLine("Starting Receive Thread........" + Environment.NewLine);
            rxThread = new Thread(new ThreadStart(RXHANDLER));
            rxThread.Start();
            Console.WriteLine("Is main thread is alive" +
                            " ? : {0}", rxThread.IsAlive);
        }

        public void CONNECT()
        {
            //TPLOCK = 1;
            REQ_ID = 0X735;
            RES_ID = 0X73D;
            status = UDSDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
            Console.WriteLine("Activate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Device Connected " + driverConfig.channel[0].name + " and " + driverConfig.channel[1].name + " active");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("TESTER REQUEST CAN ID: 0x735" + Environment.NewLine);
            Console.WriteLine("ECU RESPONSE CAN ID: 0x73D" + Environment.NewLine);
            Console.WriteLine("Starting Receive Thread........" + Environment.NewLine);
            rxThread = new Thread(new ThreadStart(RXHANDLER));
            rxThread.Start();
        }

        public void DISCONNECT()
        {
            //TPLOCK = 0;
            status = UDSDemo.XL_DeactivateChannel(portHandle, accessMask);
            Console.WriteLine("Deactivate Channel  :" + status + Environment.NewLine);
            if (status != XLDefine.XL_Status.XL_SUCCESS) PrintFunctionError();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Device status: Device disconnected ");
            Console.ForegroundColor = ConsoleColor.White;
            rxThread.Abort(); // CHECK

        }

        public void RXHANDLER()
        {
            //Boolean response = false;
            // Create new object containing received data 
            XLClass.xl_event receivedEvent = new XLClass.xl_event();
            // Result of XL Driver function calls
            XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;
            // Note: this thread will be destroyed by MAIN
            while (true)
            {
                // Wait for hardware events
                if (xlEvWaitHandle.WaitOne(5000))
                {
                    // ...init xlStatus first
                    xlStatus = XLDefine.XL_Status.XL_SUCCESS;
                    // afterwards: while hw queue is not empty...
                    
                    while (xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY)
                    {
                        // ...block RX thread to generate RX-Queue overflows
                        while (blockRxThread) { Thread.Sleep(1000); }
                        // ...receive data from hardware.
                        xlStatus = UDSDemo.XL_Receive(portHandle, ref receivedEvent);
                        //  If receiving succeed....
                        if (xlStatus == XLDefine.XL_Status.XL_SUCCESS)
                        {
                           
                            if ((receivedEvent.flags & XLDefine.XL_MessageFlags.XL_EVENT_FLAG_OVERRUN) != 0)
                            {

                            }
                            // ...and data is a Rx msg...
                            if (receivedEvent.tag == XLDefine.XL_EventTags.XL_RECEIVE_MSG)
                            {
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0)
                                {

                                }
                                // ...check various flags
                                if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                {

                                    Console.WriteLine("Error frame" + Environment.NewLine);



                                }
                                else if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                    == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                {

                                    Console.WriteLine("Remote frame" + Environment.NewLine);

                                }
                                else if ((receivedEvent.tagData.can_Msg.id == RES_ID) && (receivedEvent.chanIndex == 2))
                                {
                                    RTrigger = 1;
                                   
                                    switch (receivedEvent.tagData.can_Msg.data[1])
                                    {
                                        case 0X7F:                                     //for negative responses

                                            Console.WriteLine("Negative Response Received!" + Environment.NewLine);    // works fine

                                            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", receivedEvent.tagData.can_Msg.id, receivedEvent.tagData.can_Msg.dlc,
                                            receivedEvent.tagData.can_Msg.data[0], receivedEvent.tagData.can_Msg.data[1], receivedEvent.tagData.can_Msg.data[2], receivedEvent.tagData.can_Msg.data[3],
                                            receivedEvent.tagData.can_Msg.data[4], receivedEvent.tagData.can_Msg.data[5], receivedEvent.tagData.can_Msg.data[6], receivedEvent.tagData.can_Msg.data[7]);
                                            Console.ForegroundColor = ConsoleColor.DarkRed;
                                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            response = false;

                                            break;
                                        default:                                       //for all other responses                      

                                            Console.WriteLine("Response Received, Positive Response" + Environment.NewLine);    // works fine
                                            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", receivedEvent.tagData.can_Msg.id, receivedEvent.tagData.can_Msg.dlc,
                                            receivedEvent.tagData.can_Msg.data[0], receivedEvent.tagData.can_Msg.data[1], receivedEvent.tagData.can_Msg.data[2], receivedEvent.tagData.can_Msg.data[3],
                                            receivedEvent.tagData.can_Msg.data[4], receivedEvent.tagData.can_Msg.data[5], receivedEvent.tagData.can_Msg.data[6], receivedEvent.tagData.can_Msg.data[7]);

                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            response = true;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
        }
        ////////////////// END OF RXHANDLER //////////////////////////

        ////////////////// START OF TXHANDLER ////////////////////////
        public void TXHANDLER(int SUB_FN)
        {
            switch (SUB_FN)
            {
                case 0X10:
                    //string nula_deset;
                    //Console.WriteLine("Unesite sub funkciju (0,1,2): ");
                    //nula_deset = Console.ReadLine();
                    TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X01, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    // DID = Int32.Parse(textBox5.Text, System.Globalization.NumberStyles.HexNumber);
                    /*if (Int32.Parse(nula_deset, System.Globalization.NumberStyles.HexNumber) == 0)
                    {
                        
                        //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        //P2TIMER.Start();
                    }
                    else if (Int32.Parse(nula_deset, System.Globalization.NumberStyles.HexNumber) == 1)
                    {
                        TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X02, FILLER, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                        //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        //P2TIMER.Start();
                    }
                    else if (Int32.Parse(nula_deset, System.Globalization.NumberStyles.HexNumber) == 2)
                    {
                        TXBuffFill(REQ_ID, 8, 0X02, 0X10, 0X03, FILLER, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                        //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        //P2TIMER.Start();
                    }*/
                    break;
                case 0X11:
                    TXBuffFill(REQ_ID, 8, 0X02, 0X11, 0X02, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    /*string nula_jedanaest;
                    Console.WriteLine("Unesite sub funkciju (0,1,2): ");
                    nula_jedanaest = Console.ReadLine();

                    if (Int32.Parse(nula_jedanaest, System.Globalization.NumberStyles.HexNumber) == 0)
                    {
                        TXBuffFill(REQ_ID, 8, 0X02, 0X11, 0X01, FILLER, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                        //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        //P2TIMER.Start();
                    }
                    else if (Int32.Parse(nula_jedanaest, System.Globalization.NumberStyles.HexNumber) == 1)
                    {
                        
                        // P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        //P2TIMER.Start();
                    }
                    else if (Int32.Parse(nula_jedanaest, System.Globalization.NumberStyles.HexNumber) == 2)
                    {
                        TXBuffFill(REQ_ID, 8, 0X02, 0X11, 0X03, FILLER, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                        // P2TIMER.Interval =P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        // P2TIMER.Start();
                    }
                    break;
                case 0X3E:
                    
                  
                        TXBuffFill(REQ_ID, 8, 0X02, 0X3E, 0x00, FILLER, FILLER, FILLER, FILLER, FILLER);
                        txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                        TRACER(1, 0);
                        // P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                        // P2TIMER.Start();
                   */

                    break;
                case 0X22:
                    string nula_22;
                    Console.WriteLine("Unesite DID: ");
                    nula_22 = Console.ReadLine();
                    if (String.IsNullOrEmpty(nula_22))
                    {
                        Console.WriteLine("DID Cannot be empty!");
                    }
                    else
                    {
                        int DID;
                        //DID = byte.Parse(textBox5.Text);
                        DID = Int32.Parse(nula_22, System.Globalization.NumberStyles.HexNumber);
                        if ((DID <= 0xFFFF) && (DID > 0X0000))
                        {
                            DIDHIGH = (byte)((DID >> 8) & 0XFF);
                            DIDLOW = (byte)(DID & 0XFF);
                            TXBuffFill(REQ_ID, 8, 0X03, 0X22, DIDHIGH, DIDLOW, FILLER, FILLER, FILLER, FILLER);
                            txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                            TRACER(1, 0);
                            // P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                            // P2TIMER.Start();
                        }
                        else
                        {
                            Console.WriteLine("DID is not valid!", "DID Error");
                        }
                    }
                    break;

                case 0X14:
                    TXBuffFill(REQ_ID, 8, 0X01, 0X14, FILLER, FILLER, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                    // P2TIMER.Start();
                    break;
                case 0X19:
                    string mask;
                    Console.WriteLine("\n Unesite Masku za dati servis: ");
                    mask = Console.ReadLine();
                    int Mask = Int32.Parse(mask, System.Globalization.NumberStyles.HexNumber);
                    if ((Mask > 0XFF) || (Mask < 0X00))
                    {
                        Console.WriteLine("Mask Value not valid!", "Warning");
                    }
                    TXBuffFill(REQ_ID, 8, 0X03, 0X19, 0X01, Mask, FILLER, FILLER, FILLER, FILLER);
                    txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                    TRACER(1, 0);
                    //P2TIMER.Interval = P2;                                                   //(type,direction) ==> type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
                    // P2TIMER.Start();
                    break;
            }
        }


        ////////////////// END OF TXHANDLER //////////////////////////
        ///
        /////////////////////////////////////////////////////////////////////

        public bool P2tick()
        {
            if (RTrigger == 0)
            {
                Console.WriteLine("P2 Timer Expired! No response from ECU" + Environment.NewLine);
                return false;
            }
            else if (RTrigger == 1)
            {
                RTrigger = 0;
            }
            //P2TIMER.Stop();

            return true;
        }

        /////////////////////////////////////////////////////////////////////////

        /*private void TPtick()
        {
            if (TPLOCK == 1)
            {
                TXBuffFill(REQ_ID, 8, 0X01, 0X3E, FILLER, FILLER, FILLER, FILLER, FILLER, FILLER);
                txStatus = UDSDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
                TRACER(1, 0);
            }
            else if (TPLOCK == 0)
            {

            }
        }*/
        /////////////////////////////////////////////////////////////////////////
        ///
        public void TXBuffFill(uint id, ushort dlc, int PCI, int SID_RQ, int DATA_A, int DATA_B, int DATA_C, int DATA_D, int DATA_E, int DATA_F)
        {
            xlEventCollection.xlEvent[0].tagData.can_Msg.id = id;
            xlEventCollection.xlEvent[0].tagData.can_Msg.dlc = dlc;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[0] = (byte)PCI;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[1] = (byte)SID_RQ;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[2] = (byte)DATA_A;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[3] = (byte)DATA_B;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[4] = (byte)DATA_C;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[5] = (byte)DATA_D;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[6] = (byte)DATA_E;
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[7] = (byte)DATA_F;
            xlEventCollection.xlEvent[0].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;

            TRACE_DATA = string.Format(" {0:X2} {1} {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} ", xlEventCollection.xlEvent[0].tagData.can_Msg.id, xlEventCollection.xlEvent[0].tagData.can_Msg.dlc,
                xlEventCollection.xlEvent[0].tagData.can_Msg.data[0], xlEventCollection.xlEvent[0].tagData.can_Msg.data[1], xlEventCollection.xlEvent[0].tagData.can_Msg.data[2], xlEventCollection.xlEvent[0].tagData.can_Msg.data[3],
                xlEventCollection.xlEvent[0].tagData.can_Msg.data[4], xlEventCollection.xlEvent[0].tagData.can_Msg.data[5], xlEventCollection.xlEvent[0].tagData.can_Msg.data[6], xlEventCollection.xlEvent[0].tagData.can_Msg.data[7]);
        }
        /////////////////////////////////////////////////////////////////////////
        ///

        public void TRACER(int type, int direction)
        {
            switch (direction)    // type 1 - positive event, type 2 - negative event, direction 0 - tx, direction 1 - rx 
            {
                case 0:
                    switch (type)
                    {
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("TX: " + txStatus + Environment.NewLine);
                            Console.WriteLine("TX Data: " + TRACE_DATA + Environment.NewLine);
                            break;
                    }
                    break;
                case 1:
                    switch (type)
                    {
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("RX: " + xlStatus + Environment.NewLine);
                            Console.WriteLine("RX Data: " + TRACE_DATA + Environment.NewLine);

                            break;
                        case 2:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("RX: " + Environment.NewLine);//xlStatus + Environment.NewLine;
                            Console.WriteLine("RX Data: " + Environment.NewLine);//TRACE_DATA + Environment.NewLine;
                            break;
                    }
                    break;
            }



        }
        /////////////////////////////////////////////////////////////////////////
        ///
        public bool GetAppChannelAndTestIsOk(uint appChIdx, ref UInt64 chMask, ref int chIdx)
        {
            XLDefine.XL_Status status = UDSDemo.XL_GetApplConfig(appName, appChIdx, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            {
                Console.WriteLine("XL_Get application Configuration:" + status + Environment.NewLine);
            }

            chMask = UDSDemo.XL_GetChannelMask(hwType, (int)hwIndex, (int)hwChannel);
            chIdx = UDSDemo.XL_GetChannelIndex(hwType, (int)hwIndex, (int)hwChannel);
            if (chIdx < 0 || chIdx >= driverConfig.channelCount)
            {
                // the (hwType, hwIndex, hwChannel) triplet stored in the application configuration does not refer to any available channel.
                return false;
            }

            // test if CAN is available on this channel
            return (driverConfig.channel[chIdx].channelBusCapabilities & XLDefine.XL_BusCapabilities.XL_BUS_ACTIVE_CAP_CAN) != 0;
            /////////////////////////////////////////////////////////////////////////
            ///


        }
    }
}
