const apiUrl = process.env.PETCLINIC_API_URL;

if (!apiUrl) {
  throw new Error("PETCLINIC_API_URL is required. Start the application with Aspire.");
}

module.exports = {
  "/api": {
    target: apiUrl,
    changeOrigin: true,
  },
};
