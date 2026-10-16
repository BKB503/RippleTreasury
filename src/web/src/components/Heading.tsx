import { Box, Typography } from "@mui/material";
export function Heading({
  title,
  subtitle,
}: {
  title: string;
  subtitle: string;
}) {
  return (
    <Box mb={3}>
      <Typography variant="overline" color="primary">
        PAYMENT OPERATIONS
      </Typography>
      <Typography variant="h4">{title}</Typography>
      <Typography color="text.secondary" mt={1}>
        {subtitle}
      </Typography>
    </Box>
  );
}
