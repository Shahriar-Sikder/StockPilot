// Small shared facts about the signed-in workspace, set once by the shell (dashboard/index.jsx).
// Used where the mockup prints the tenant name: PDF exports and printed reports.
let info = { tenantName: '', userName: '' };

export const setWorkspaceInfo = (value) => { info = { ...info, ...value }; };
export const getWorkspaceInfo = () => info;
