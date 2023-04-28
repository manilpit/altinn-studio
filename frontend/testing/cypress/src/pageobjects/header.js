//Selectors in header of studio common to all pages
export const header = {
  profileIcon: '#profile-icon-button',
  profileIconDesigner: "img[aria-label*='profilikon']",
  menu: {
    item: '[role="menuitem"]',
    all: '#menu-all',
    self: '#menu-self',
    org: "[id='menu-org*']",
    gitea: '#menu-gitea',
    logOut: '#menu-logout',
    openRepo: 'a[href*="repos"]',
    docs: 'a[href="https://docs.altinn.studio/"]',
  },
};
