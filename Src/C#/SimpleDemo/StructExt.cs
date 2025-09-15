using System.Runtime.InteropServices;

namespace Connect;

public static class StructExt
{
    public static byte[] StructToBytes<T>(this T structure) where T : struct
    {
        var size   = Marshal.SizeOf(structure);
        var buffer = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(structure, buffer, false);
            var bytes = new byte[size];
            Marshal.Copy(buffer, bytes, 0, size);

            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }


    public static T BytesToStruct<T>(this byte[] bytes, int size = 0) where T : struct
    {
        if (size == 0)
        {
            size = Marshal.SizeOf(typeof(T));
        }

        var buffer = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.Copy(bytes, 0, buffer, size);

            return (T)(Marshal.PtrToStructure(buffer, typeof(T)) ?? throw new InvalidOperationException());
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}