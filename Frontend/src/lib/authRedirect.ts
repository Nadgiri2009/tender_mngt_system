export function getAuthReturnUrl(): string | null {
  if (typeof window === "undefined") return null;

  const requestedUrl = new URLSearchParams(window.location.search).get("returnUrl");
  if (!requestedUrl || !requestedUrl.startsWith("/") || requestedUrl.startsWith("//") || requestedUrl.includes("\\")) {
    return null;
  }

  const parsedUrl = new URL(requestedUrl, window.location.origin);
  return parsedUrl.origin === window.location.origin
    ? `${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`
    : null;
}
