import { createTheme } from "@mui/material";
export const theme = createTheme({
  palette: {
    primary: { main: "#154c45" },
    secondary: { main: "#bf8a43" },
    background: { default: "#f3f5f4", paper: "#fff" },
  },
  typography: {
    fontFamily: "Inter, Segoe UI, sans-serif",
    h4: { fontWeight: 650 },
    h6: { fontWeight: 650 },
  },
  shape: { borderRadius: 10 },
  components: {
    MuiButton: { styleOverrides: { root: { textTransform: "none" } } },
    MuiCard: {
      styleOverrides: {
        root: { boxShadow: "none", border: "1px solid #dde4e1" },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: { color: "#65756e", fontWeight: 600, background: "#f7f9f8" },
      },
    },
  },
});
