//	Copyright © 2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
//	Author: Roger VUISTINER, Maintainer: Roger VUISTINER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Avoid error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported.
    /// </summary>
    /// <remarks>
    /// See <see href="https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined"/>
    /// </remarks>
    internal static class IsExternalInit { }
}