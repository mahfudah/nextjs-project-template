<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Events\PrintJobEvent;

class PrintController extends Controller
{
    public function sendPrintJob(Request $request)
    {
        $this->validate($request, [
            'rawData' => 'required|string',
            'printer' => 'required|string'
        ]);

        // Broadcast the print job event
        broadcast(new PrintJobEvent($request->rawData, $request->printer))->toOthers();

        return response()->json([
            'status' => 'success',
            'message' => 'Print job dispatched successfully'
        ]);
    }

    public function testPrint(Request $request)
    {
        $this->validate($request, [
            'printer' => 'required|string'
        ]);

        // Create a test print string with Epson ESC/P commands
        $testData = "\x1B@"; // Initialize printer
        $testData .= "\x1BE\x01"; // Bold ON
        $testData .= "TEST PRINT\n";
        $testData .= "\x1BE\x00"; // Bold OFF
        $testData .= "------------------------\n";
        $testData .= "Date: " . date('Y-m-d H:i:s') . "\n";
        $testData .= "Printer: " . $request->printer . "\n";
        $testData .= "------------------------\n\n";
        $testData .= "Print Test Successful!\n\n\n\n\n";
        $testData .= "\x1D\x56\x41\x03"; // Cut paper (if supported)

        // Broadcast the test print job
        broadcast(new PrintJobEvent($testData, $request->printer))->toOthers();

        return response()->json([
            'status' => 'success',
            'message' => 'Test print job dispatched'
        ]);
    }
}
