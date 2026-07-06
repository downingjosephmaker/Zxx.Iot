export const baseUrlApi = (url: string) =>
  process.env.NODE_ENV === "development"
    ? `http://192.168.0.76:13696/Api${url}`
    : `http://192.168.0.76:13696/Api${url}`;
