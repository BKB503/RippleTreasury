import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import {
  AppBar,
  Box,
  Button,
  Chip,
  Container,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import { DemoUserSelector } from "../features/demo-user/DemoUserSelector";
export function OperationsLayout({ children }: { children: ReactNode }) {
  return (
    <>
      {" "}
      <AppBar position="static" elevation={0} sx={{ background: "#123e38" }}>
        <Toolbar sx={{ gap: 4 }}>
          <Box>
            <Typography fontWeight={700} letterSpacing={1.3}>
              RIPPLE TREASURY
            </Typography>
            <Typography variant="caption" sx={{ opacity: 0.65 }}>
              OUTBOUND PAYMENT OPERATIONS
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} flex={1}>
            {[
              ["/", "Dashboard & Reports"],
              ["/process-batch", "Process batch"],
              ["/batches", "Batches"],
              ["/reconciliation", "Reconciliation"],
            ].map(([path, label]) => (
              <Button
                key={path}
                component={NavLink}
                to={path}
                end={path === "/"}
                sx={{
                  color: "#d8e6e1",
                  "&.active": { background: "#ffffff18", color: "#fff" },
                }}
              >
                {label}
              </Button>
            ))}
          </Stack>
          <DemoUserSelector />
          <Chip
            size="small"
            label="DEMO ENVIRONMENT"
            sx={{ color: "#e4d1af", border: "1px solid #8b8066" }}
          />
        </Toolbar>
      </AppBar>
      <Container maxWidth="xl" sx={{ py: 4 }}>
        {children}
        <Typography
          variant="caption"
          color="text.secondary"
          display="block"
          mt={4}
        >
          Demonstration environment | No real transfers | Bank acceptance is not
          settlement
        </Typography>
      </Container>
    </>
  );
}
