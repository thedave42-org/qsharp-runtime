// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Intrinsic {
    open Microsoft.Quantum.Diagnostics;

    @EnableTestingViaName("Test.TargetDefinitions.PreparePostM")
    internal operation PreparePostM(result : Result, qubit : Qubit) : Unit {
        // This platform does not require any post-measurement reset, so
        // no additional work is needed.
    }
}