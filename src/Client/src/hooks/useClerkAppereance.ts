import { useTheme } from "@mui/material";
import { useMemo } from "react";

export const useClerkAppereance = () => {
  const theme = useTheme();
  return useMemo(
    () => ({
      variables: {
        colorPrimary: theme.palette.primary.main,
        colorText: theme.palette.text.primary,
        colorBackground: theme.palette.background.default,
        borderRadius: `${theme.shape.borderRadius}px`,
        fontFamily: theme.typography.fontFamily,
      },
      elements: {
        formButtonPrimary: {
          backgroundColor: theme.palette.primary.main,
          "&:hover": {
            backgroundColor: theme.palette.primary.dark,
          },
        },
        card: {
          boxShadow: theme.shadows[4],
          borderRadius: `${theme.shape.borderRadius}px`,
        },
      },
    }),
    [theme]
  );
};
