using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Mvc;

namespace TestWebApp.Controllers
{
    public class HomeController : Controller
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        protected static Type MakeDelegateType(Type returntype, List<Type> paramtypes)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "DynamicDelegate";
            AppDomain thisDomain = Thread.GetDomain();
            AssemblyBuilder asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder dynamicMod = asmBuilder.DefineDynamicModule(asmBuilder.GetName().Name, false);

            TypeBuilder tb = dynamicMod.DefineType("delegate-maker" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Sealed, typeof(MulticastDelegate));

            tb.DefineConstructor(MethodAttributes.RTSpecialName |
                 MethodAttributes.SpecialName | MethodAttributes.Public |
                 MethodAttributes.HideBySig, CallingConventions.Standard,
                 new Type[] { typeof(object), typeof(IntPtr) }).
                 SetImplementationFlags(MethodImplAttributes.Runtime);

            var inv = tb.DefineMethod("Invoke", MethodAttributes.Public |
                 MethodAttributes.Virtual | MethodAttributes.NewSlot |
                 MethodAttributes.HideBySig,
                 CallingConventions.Standard, returntype, null,
                 new Type[]
                 { 

          typeof(System.Runtime.CompilerServices.CallConvCdecl)
                 },
                 paramtypes.ToArray(), null, null);

            inv.SetImplementationFlags(MethodImplAttributes.Runtime);

            var t = tb.CreateType();
            return t;
        }

        public ActionResult Index()
        {
             // load the DLL
             IntPtr Handle = LoadLibrary("SomeThirdPartyLib.dll");
             if (Handle == IntPtr.Zero)
             {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("Failed to load library (ErrorCode: {0})", errorCode));
            }

            // get the pointer address of the method 'addnumbers' 
            IntPtr funcaddr = GetProcAddress(Handle, "addnumbers");

            // create delegate of the method from dll
            // with the signature 'int addnumbers(int, int)'
            Type delegateType = MakeDelegateType(typeof(int), new List<Type> { typeof(int), typeof(int) });
            Delegate addnumbers = Marshal.GetDelegateForFunctionPointer(funcaddr, delegateType);

            // Execute the method using 'DynamicInvoke'
            int res = (int) addnumbers.DynamicInvoke(10,20);

            ViewBag.Message = "The result is " + res;

            // Free
            if (Handle != IntPtr.Zero)
                FreeLibrary(Handle);

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}