import { BrowserRouter, Route, Routes } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { queryClient } from "./app/queryClient";
import { theme } from "./app/theme";
import { OperationsLayout } from "./app/OperationsLayout";
import { DemoUserProvider } from "./features/demo-user/DemoUserProvider";
import Overview from "./features/overview/Overview";
import Batches from "./features/batches/Batches";
import BatchProcessing from "./features/batch-processing/BatchProcessing";
import Reconciliation from "./features/reconciliation/Reconciliation";
export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <DemoUserProvider>
          <BrowserRouter>
            <OperationsLayout>
              <Routes>
                <Route path="/" element={<Overview />} />
                <Route path="/process-batch" element={<BatchProcessing />} />
                <Route path="/batches" element={<Batches />} />
                <Route path="/reconciliation" element={<Reconciliation />} />
              </Routes>
            </OperationsLayout>
          </BrowserRouter>
        </DemoUserProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
