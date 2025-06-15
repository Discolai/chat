import ReactMarkdown, { type Components } from "react-markdown";
import { Typography, Paper, Link } from "@mui/material";
import "highlight.js/styles/github.css";
import hljs from "highlight.js";

interface MarkdownRendererProps {
  children: string | null | undefined;
}

const MarkdownRenderer = ({ children }: MarkdownRendererProps) => {
  const renderers: Components = {
    h1: ({ children }) => (
      <Typography variant="h1" component={"div"}>
        {children}
      </Typography>
    ),
    h2: ({ children }) => (
      <Typography variant="h2" component={"div"}>
        {children}
      </Typography>
    ),
    h3: ({ children }) => (
      <Typography variant="h3" component={"div"}>
        {children}
      </Typography>
    ),
    h4: ({ children }) => (
      <Typography variant="h4" component={"div"}>
        {children}
      </Typography>
    ),
    h5: ({ children }) => (
      <Typography variant="h5" component={"div"}>
        {children}
      </Typography>
    ),
    h6: ({ children }) => (
      <Typography variant="h6" component={"div"}>
        {children}
      </Typography>
    ),
    p: ({ children }) => (
      <Typography variant="body1" component={"div"}>
        {children}
      </Typography>
    ),
    link: ({ href, children }) => <Link href={href}>{children}</Link>,
    code: ({ children, className }) => (
      <Paper
        className={className}
        ref={(element: HTMLParagraphElement) => {
          if (!element || element.hasAttribute("data-highlighted")) {
            return;
          }
          hljs.highlightElement(element);
        }}
        elevation={3}
        sx={{ padding: 2, textWrap: "wrap" }}
      >
        {children}
      </Paper>
    ),
  };

  return <ReactMarkdown components={renderers}>{children}</ReactMarkdown>;
};

export default MarkdownRenderer;
