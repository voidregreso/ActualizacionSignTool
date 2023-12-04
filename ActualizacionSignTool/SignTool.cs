using Properties;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace ActualizacionSignTool;

internal static class SignTool
{
	private struct SIGNER_SUBJECT_INFO
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct SubjectChoiceUnion
		{
			[FieldOffset(0)]
			public IntPtr pSignerFileInfo;

			[FieldOffset(0)]
			public IntPtr pSignerBlobInfo;
		}

		public uint cbSize;

		public IntPtr pdwIndex;

		public uint dwSubjectChoice;

		public SubjectChoiceUnion Union1;
	}

	private struct SIGNER_CERT
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct SignerCertUnion
		{
			[FieldOffset(0)]
			public IntPtr pwszSpcFile;

			[FieldOffset(0)]
			public IntPtr pCertStoreInfo;

			[FieldOffset(0)]
			public IntPtr pSpcChainInfo;
		}

		public uint cbSize;

		public uint dwCertChoice;

		public SignerCertUnion Union1;

		public IntPtr hwnd;
	}

	private struct SIGNER_SIGNATURE_INFO
	{
		public uint cbSize;

		public uint algidHash;

		public uint dwAttrChoice;

		public IntPtr pAttrAuthCode;

		public IntPtr psAuthenticated;

		public IntPtr psUnauthenticated;
	}

	private struct SIGNER_FILE_INFO
	{
		public uint cbSize;

		public IntPtr pwszFileName;

		public IntPtr hFile;
	}

	private struct SIGNER_CERT_STORE_INFO
	{
		public uint cbSize;

		public IntPtr pSigningCert;

		public uint dwCertPolicy;

		public IntPtr hCertStore;
	}

	private struct SIGNER_CONTEXT
	{
		public uint cbSize;

		public uint cbBlob;

		public IntPtr pbBlob;
	}

	private struct SIGNER_PROVIDER_INFO
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct SignerProviderUnion
		{
			[FieldOffset(0)]
			public IntPtr pwszPvkFileName;

			[FieldOffset(0)]
			public IntPtr pwszKeyContainer;
		}

		public uint cbSize;

		public IntPtr pwszProviderName;

		public uint dwProviderType;

		public uint dwKeySpec;

		public uint dwPvkChoice;

		public SignerProviderUnion Union1;
	}

	[DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int SignerSign(IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo, string pwszHttpTimeStamp, IntPtr psRequest, IntPtr pSipData);

	[DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int SignerSignEx(uint dwFlags, IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo, string pwszHttpTimeStamp, IntPtr psRequest, IntPtr pSipData, out SIGNER_CONTEXT ppSignerContext);

	[DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int SignerTimeStamp(IntPtr pSubjectInfo, string pwszHttpTimeStamp, IntPtr psRequest, IntPtr pSipData);

	[DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int SignerTimeStampEx(uint dwFlags, IntPtr pSubjectInfo, string pwszHttpTimeStamp, IntPtr psRequest, IntPtr pSipData, out SIGNER_CONTEXT ppSignerContext);

	[DllImport("Crypt32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr CertCreateCertificateContext(int dwCertEncodingType, byte[] pbCertEncoded, int cbCertEncoded);

	public static bool SignWithCert(string appPath, string timestampUrl)
	{
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		IntPtr intPtr3 = IntPtr.Zero;
		IntPtr intPtr4 = IntPtr.Zero;
		try
		{
            byte[] data = (byte[])Resources.ResourceManager.GetObject("SurfacePFX");
            X509Certificate2 cert = new X509Certificate2(data);
			intPtr = CreateSignerCert(cert);
			intPtr2 = CreateSignerSubjectInfo(appPath);
			intPtr3 = CreateSignerSignatureInfo();
			intPtr4 = GetProviderInfo(cert);
			SignCode(0u, intPtr2, intPtr, intPtr3, intPtr4, out var signerContext);
			if (!string.IsNullOrEmpty(timestampUrl))
			{
				TimeStampSignedCode(0u, intPtr2, timestampUrl, out signerContext);
			}
		}
		catch (CryptographicException ex)
		{
			string msg;
			switch (Marshal.GetHRForException(ex))
			{
			case -2146885623:
				msg = string.Format("An error occurred while attempting to load the signing certificate. \"{0}\" does not appear to contain a valid certificate.", "Surface.pfx");
				break;
			case -2147024810:
                msg = $"An error occurred while attempting to load the signing certificate.  The specified password was incorrect.";
				break;
			default:
                msg = $"An error occurred while attempting to load the signing certificate.  {ex.Message}";
				break;
			}
			//MessageBox.Show(msg);
			return false;
		}
		catch (Exception ex2)
        {
            MessageBox.Show(ex2.ToString());
            return false;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr, typeof(SIGNER_CERT));
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr2, typeof(SIGNER_SUBJECT_INFO));
			}
			if (intPtr3 != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr3, typeof(SIGNER_SIGNATURE_INFO));
			}
			if (intPtr4 != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr3, typeof(SIGNER_PROVIDER_INFO));
			}
		}
		return true;
	}

	public static void SignWithThumbprint(string appPath, string thumbprint, string timestampUrl)
	{
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		IntPtr intPtr3 = IntPtr.Zero;
		IntPtr zero = IntPtr.Zero;
		try
		{
			intPtr = CreateSignerCert(thumbprint);
			intPtr2 = CreateSignerSubjectInfo(appPath);
			intPtr3 = CreateSignerSignatureInfo();
			SignCode(intPtr2, intPtr, intPtr3, zero);
			if (!string.IsNullOrEmpty(timestampUrl))
			{
				TimeStampSignedCode(intPtr2, timestampUrl);
			}
		}
		catch (CryptographicException ex)
		{
			_ = $"An error occurred while attempting to load the signing certificate.  {ex.Message}";
		}
		catch (Exception ex2)
		{
			_ = ex2.Message;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr, typeof(SIGNER_CERT));
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr2, typeof(SIGNER_SUBJECT_INFO));
			}
			if (intPtr3 != IntPtr.Zero)
			{
				Marshal.DestroyStructure(intPtr3, typeof(SIGNER_SIGNATURE_INFO));
			}
		}
	}

	private static IntPtr CreateSignerSubjectInfo(string pathToAssembly)
	{
		SIGNER_SUBJECT_INFO sIGNER_SUBJECT_INFO = default(SIGNER_SUBJECT_INFO);
		sIGNER_SUBJECT_INFO.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_SUBJECT_INFO));
		sIGNER_SUBJECT_INFO.pdwIndex = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)));
		SIGNER_SUBJECT_INFO structure = sIGNER_SUBJECT_INFO;
		Marshal.StructureToPtr(0, structure.pdwIndex, fDeleteOld: false);
		structure.dwSubjectChoice = 1u;
		IntPtr pwszFileName = Marshal.StringToHGlobalUni(pathToAssembly);
		SIGNER_FILE_INFO sIGNER_FILE_INFO = default(SIGNER_FILE_INFO);
		sIGNER_FILE_INFO.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_FILE_INFO));
		sIGNER_FILE_INFO.pwszFileName = pwszFileName;
		sIGNER_FILE_INFO.hFile = IntPtr.Zero;
		SIGNER_FILE_INFO structure2 = sIGNER_FILE_INFO;
		structure.Union1 = new SIGNER_SUBJECT_INFO.SubjectChoiceUnion
		{
			pSignerFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SIGNER_FILE_INFO)))
		};
		Marshal.StructureToPtr(structure2, structure.Union1.pSignerFileInfo, fDeleteOld: false);
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		return intPtr;
	}

	private static X509Certificate2 FindCertByThumbprint(string thumbprint)
	{
		try
		{
			string findValue = thumbprint.Replace(" ", string.Empty).ToUpperInvariant();
			X509Store[] array = new X509Store[4]
			{
				new X509Store(StoreName.My, StoreLocation.CurrentUser),
				new X509Store(StoreName.My, StoreLocation.LocalMachine),
				new X509Store(StoreName.TrustedPublisher, StoreLocation.CurrentUser),
				new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine)
			};
			foreach (X509Store x509Store in array)
			{
				x509Store.Open(OpenFlags.ReadOnly);
				X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, findValue, validOnly: false);
				x509Store.Close();
				if (x509Certificate2Collection.Count >= 1)
				{
					return x509Certificate2Collection[0];
				}
			}
			throw new Exception($"A certificate matching the thumbprint: \"{thumbprint}\" could not be found.  Make sure that a valid certificate matching the provided thumbprint is installed.");
		}
		catch (Exception ex)
		{
			throw new Exception($"{ex.Message}");
		}
	}

	private static IntPtr CreateSignerCert(X509Certificate2 cert)
	{
		SIGNER_CERT sIGNER_CERT = default(SIGNER_CERT);
		sIGNER_CERT.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT));
		sIGNER_CERT.dwCertChoice = 2u;
		sIGNER_CERT.Union1 = new SIGNER_CERT.SignerCertUnion
		{
			pCertStoreInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO)))
		};
		sIGNER_CERT.hwnd = IntPtr.Zero;
		SIGNER_CERT structure = sIGNER_CERT;
		IntPtr pSigningCert = CertCreateCertificateContext(65537, cert.GetRawCertData(), cert.GetRawCertData().Length);
		SIGNER_CERT_STORE_INFO structure2 = default(SIGNER_CERT_STORE_INFO);
		structure2.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO));
		structure2.pSigningCert = pSigningCert;
		structure2.dwCertPolicy = 2u;
		structure2.hCertStore = IntPtr.Zero;
		Marshal.StructureToPtr(structure2, structure.Union1.pCertStoreInfo, fDeleteOld: false);
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		return intPtr;
	}

	private static IntPtr CreateSignerCert(string thumbprint)
	{
		SIGNER_CERT sIGNER_CERT = default(SIGNER_CERT);
		sIGNER_CERT.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT));
		sIGNER_CERT.dwCertChoice = 2u;
		sIGNER_CERT.Union1 = new SIGNER_CERT.SignerCertUnion
		{
			pCertStoreInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO)))
		};
		sIGNER_CERT.hwnd = IntPtr.Zero;
		SIGNER_CERT structure = sIGNER_CERT;
		X509Certificate2 x509Certificate = FindCertByThumbprint(thumbprint);
		IntPtr pSigningCert = CertCreateCertificateContext(65537, x509Certificate.GetRawCertData(), x509Certificate.GetRawCertData().Length);
		SIGNER_CERT_STORE_INFO structure2 = default(SIGNER_CERT_STORE_INFO);
		structure2.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO));
		structure2.pSigningCert = pSigningCert;
		structure2.dwCertPolicy = 2u;
		structure2.hCertStore = IntPtr.Zero;
		Marshal.StructureToPtr(structure2, structure.Union1.pCertStoreInfo, fDeleteOld: false);
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		return intPtr;
	}

	private static IntPtr CreateSignerSignatureInfo()
	{
		SIGNER_SIGNATURE_INFO sIGNER_SIGNATURE_INFO = default(SIGNER_SIGNATURE_INFO);
		sIGNER_SIGNATURE_INFO.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_SIGNATURE_INFO));
		sIGNER_SIGNATURE_INFO.algidHash = 32780u;
		sIGNER_SIGNATURE_INFO.dwAttrChoice = 0u;
		sIGNER_SIGNATURE_INFO.pAttrAuthCode = IntPtr.Zero;
		sIGNER_SIGNATURE_INFO.psAuthenticated = IntPtr.Zero;
		sIGNER_SIGNATURE_INFO.psUnauthenticated = IntPtr.Zero;
		SIGNER_SIGNATURE_INFO structure = sIGNER_SIGNATURE_INFO;
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		return intPtr;
	}

	private static IntPtr GetProviderInfo(X509Certificate2 cert)
	{
		if (cert == null || !cert.HasPrivateKey)
		{
			return IntPtr.Zero;
		}
		ICspAsymmetricAlgorithm cspAsymmetricAlgorithm = (ICspAsymmetricAlgorithm)cert.PrivateKey;
		if (cspAsymmetricAlgorithm == null)
		{
			return IntPtr.Zero;
		}
		SIGNER_PROVIDER_INFO sIGNER_PROVIDER_INFO = default(SIGNER_PROVIDER_INFO);
		sIGNER_PROVIDER_INFO.cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_PROVIDER_INFO));
		sIGNER_PROVIDER_INFO.pwszProviderName = Marshal.StringToHGlobalUni(cspAsymmetricAlgorithm.CspKeyContainerInfo.ProviderName);
		sIGNER_PROVIDER_INFO.dwProviderType = (uint)cspAsymmetricAlgorithm.CspKeyContainerInfo.ProviderType;
		sIGNER_PROVIDER_INFO.dwPvkChoice = 2u;
		sIGNER_PROVIDER_INFO.Union1 = new SIGNER_PROVIDER_INFO.SignerProviderUnion
		{
			pwszKeyContainer = Marshal.StringToHGlobalUni(cspAsymmetricAlgorithm.CspKeyContainerInfo.KeyContainerName)
		};
		SIGNER_PROVIDER_INFO structure = sIGNER_PROVIDER_INFO;
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
		Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
		return intPtr;
	}

	private static void SignCode(IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo)
	{
		if (SignerSign(pSubjectInfo, pSignerCert, pSignatureInfo, pProviderInfo, null, IntPtr.Zero, IntPtr.Zero) != 0)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
		}
	}

	private static void SignCode(uint dwFlags, IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo, out SIGNER_CONTEXT signerContext)
	{
		if (SignerSignEx(dwFlags, pSubjectInfo, pSignerCert, pSignatureInfo, pProviderInfo, null, IntPtr.Zero, IntPtr.Zero, out signerContext) != 0)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
		}
	}

	private static void TimeStampSignedCode(IntPtr pSubjectInfo, string timestampUrl)
	{
		if (SignerTimeStamp(pSubjectInfo, timestampUrl, IntPtr.Zero, IntPtr.Zero) != 0)
		{
			throw new Exception($"\"{timestampUrl}\" could not be used at this time.  If necessary, check the timestampUrl, internet connection, and try again.");
		}
	}

	private static void TimeStampSignedCode(uint dwFlags, IntPtr pSubjectInfo, string timestampUrl, out SIGNER_CONTEXT signerContext)
	{
		int res = 0;
		if ((res = SignerTimeStampEx(dwFlags, pSubjectInfo, timestampUrl, IntPtr.Zero, IntPtr.Zero, out signerContext)) != 0)
		{
			throw new Exception($"\"{timestampUrl}\" could not be used at this time.  If necessary, " +
				$"check the timestampUrl, internet connection, and try again." + "Code: " + Marshal.GetHRForLastWin32Error()
				+ ", Return value: " + res);
		}
	}
}
