import struct
import os

def build_delivery_fee_dll(output_path):
    dos_header = bytearray(0x40)
    dos_header[0:2] = b'MZ'
    struct.pack_into('<I', dos_header, 0x3C, 0x80)

    pe_sig = b'PE\0\0'

    coff = struct.pack('<HHIIIHH',
        0x8664, 2, 0, 0, 0, 240, 0x2022
    )

    opt_std = struct.pack('<HBBIIIII',
        0x020B, 14, 0, 0x200, 0x200, 0, 0x1000, 0x1000
    )

    opt_win = struct.pack('<QIIHHHHHHIIIIHHQQQQII',
        0x180000000, 0x1000, 0x200,
        6, 0, 0, 0, 6, 0, 0,
        0x3000, 0x400, 0, 3, 0x0160,
        0x100000, 0x1000, 0x100000, 0x1000, 0, 16
    )

    data_dirs = bytearray(128)
    struct.pack_into('<II', data_dirs, 0, 0x2000, 0x200)

    sec_text = struct.pack('<8sIIIIIIHHI',
        b'.text\0\0\0', 0x100, 0x1000, 0x200, 0x400,
        0, 0, 0, 0, 0x60000020
    )

    sec_edata = struct.pack('<8sIIIIIIHHI',
        b'.edata\0\0', 0x100, 0x2000, 0x200, 0x600,
        0, 0, 0, 0, 0x40000040
    )

    header_block = bytearray(0x400)
    header_block[0:0x40] = dos_header
    pos = 0x80
    header_block[pos:pos+4] = pe_sig; pos += 4
    header_block[pos:pos+20] = coff; pos += 20
    header_block[pos:pos+len(opt_std)] = opt_std; pos += len(opt_std)
    header_block[pos:pos+len(opt_win)] = opt_win; pos += len(opt_win)
    header_block[pos:pos+len(data_dirs)] = data_dirs; pos += len(data_dirs)
    header_block[pos:pos+40] = sec_text; pos += 40
    header_block[pos:pos+40] = sec_edata; pos += 40

    code = bytearray(0x200)
    # DllMain: mov eax, 1; ret
    code[0:6] = bytes([0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3])

    # calculate_delivery_fee(ecx=distance, edx=weight, r8d=fragile) -> eax
    func_bytes = bytes([
        0x85, 0xC9,                         # test ecx, ecx
        0x7E, 0x20,                         # jle err
        0x85, 0xD2,                         # test edx, edx
        0x7E, 0x1C,                         # jle err
        0x6B, 0xC9, 0x0A,                   # imul ecx, ecx, 10
        0x6B, 0xD2, 0x14,                   # imul edx, edx, 20
        0x01, 0xD1,                         # add ecx, edx
        0x81, 0xC1, 0xF4, 0x01, 0x00, 0x00, # add ecx, 500
        0x45, 0x85, 0xC0,                   # test r8d, r8d
        0x74, 0x06,                         # jz done
        0x81, 0xC1, 0x2C, 0x01, 0x00, 0x00, # add ecx, 300
        0x89, 0xC8,                         # done: mov eax, ecx
        0xC3,                               # ret
        0xB8, 0xFF, 0xFF, 0xFF, 0xFF,       # err: mov eax, -1
        0xC3                                # ret
    ])
    code[0x10:0x10+len(func_bytes)] = func_bytes

    edata = bytearray(0x200)
    exp_dir = struct.pack('<IIHHIIIIIII',
        0, 0, 0, 0,
        0x2040, 1, 1, 1,
        0x2028, 0x202C, 0x2030
    )
    edata[0:40] = exp_dir
    struct.pack_into('<I', edata, 0x28, 0x1010)
    struct.pack_into('<I', edata, 0x2C, 0x2060)
    struct.pack_into('<H', edata, 0x30, 0)

    dll_name = b'delivery_fee.dll\0'
    edata[0x40:0x40+len(dll_name)] = dll_name

    func_name = b'calculate_delivery_fee\0'
    edata[0x60:0x60+len(func_name)] = func_name

    full_dll = header_block + code + edata
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with open(output_path, 'wb') as f:
        f.write(full_dll)
    print(f"Built native DLL: {output_path} ({len(full_dll)} bytes)")

if __name__ == '__main__':
    base_dir = os.path.dirname(os.path.abspath(__file__))
    dll_path = os.path.join(base_dir, 'delivery_fee.dll')
    build_delivery_fee_dll(dll_path)
