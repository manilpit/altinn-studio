import React, { useEffect, useState } from 'react';
import { styled, StylesProvider } from '@mui/styles';
import { ThemeProvider } from '@mui/material';
import { Route, Routes } from 'react-router-dom';
import AltinnSpinner from 'app-shared/components/AltinnSpinner';
import { AltinnButton } from 'app-shared/components';
import { post } from 'app-shared/utils/networking';
import { getLanguageFromKey } from 'app-shared/utils/language';
import { DashboardActions, SelectedContext } from '../resources/fetchDashboardResources/dashboardSlice';
import { fetchLanguage } from '../resources/fetchLanguage/languageSlice';
import type { IHeaderContext } from 'app-shared/navigation/main-header/Header';
import Header, { HeaderContext, SelectedContextType } from 'app-shared/navigation/main-header/Header';

import { userHasAccessToSelectedContext } from 'common/utils';
import { generateClassName, theme } from 'common/utils/muiUtils';
import { useAppDispatch, useAppSelector } from 'common/hooks';
import { CenterContainer } from 'common/components/CenterContainer';
import { Footer } from 'common/components/Footer';
import StandaloneDataModelling from 'features/standaloneDataModelling/DataModelling';
import { useGetOrganizationsQuery } from 'services/organizationApi';
import { Dashboard } from 'features/dashboard';
import { CreateService } from 'features/createService/CreateService';

import './App.css';

const Root = styled('div')(() => ({
  height: '100vh',
  display: 'grid',
  gridTemplateRows: 'auto 1fr',
}));

export const App = () => {
  const dispatch = useAppDispatch();
  const user = useAppSelector((state) => state.dashboard.user);
  const language = useAppSelector((state) => state.language.language);
  const selectedContext = useAppSelector((state) => state.dashboard.selectedContext);
  const { data: orgs = [], isLoading: isLoadingOrganizations } = useGetOrganizationsQuery();

  const setSelectedContext = (newSelectedContext: SelectedContext) => {
    dispatch(
      DashboardActions.setSelectedContext({
        selectedContext: newSelectedContext,
      }),
    );
  };

  if (!isLoadingOrganizations && !userHasAccessToSelectedContext({ selectedContext, orgs })) {
    setSelectedContext(SelectedContextType.Self);
  }

  const headerContextValue: IHeaderContext = {
    selectableOrgs: orgs,
    selectedContext,
    setSelectedContext,
    user,
  };

  useEffect(() => {
    dispatch(
      DashboardActions.fetchCurrentUser({
        url: `${window.location.origin}/designer/api/v1/user/current`,
      }),
    );

    dispatch(
      fetchLanguage({
        url: `${window.location.origin}/designerapi/Language/GetLanguageAsJSON`,
        languageCode: 'nb',
      }),
    );

    dispatch(
      DashboardActions.fetchServices({
        url: `${window.location.origin}/designer/api/v1/user/repos`,
      }),
    );
  }, [dispatch]);

  const [showLogOutButton, setShowLogoutButton] = useState(false);
  useEffect(() => {
    const timer = setTimeout(() => {
      if (!user) {
        setShowLogoutButton(true);
      }
    }, 5000);

    return () => {
      clearTimeout(timer);
    };
  }, [user]);

  return (
    <StylesProvider generateClassName={generateClassName}>
      <ThemeProvider theme={theme}>
        {user && !isLoadingOrganizations ? (
          <Root>
            <HeaderContext.Provider value={headerContextValue}>
              <Header language={language} />
            </HeaderContext.Provider>
            <Routes>
              <Route
                path='/'
                element={
                  <>
                    <CenterContainer>
                      <Dashboard />
                    </CenterContainer>
                    <Footer />
                  </>
                }
              />
              <Route path='/datamodelling/:org/:repoName' element={<StandaloneDataModelling language={language} />} />
              <Route path='/new' element={<CreateService />} />
            </Routes>
          </Root>
        ) : (
          <CenterContainer>
            <AltinnSpinner spinnerText={getLanguageFromKey('dashboard.loading', language)} />
            {showLogOutButton && (
              <AltinnButton
                onClickFunction={() =>
                  post(`${window.location.origin}/repos/user/logout`).then(() => {
                    window.location.assign(`${window.location.origin}/Home/Logout`);
                  })
                }
                btnText={getLanguageFromKey('dashboard.logout', language)}
              />
            )}
          </CenterContainer>
        )}
      </ThemeProvider>
    </StylesProvider>
  );
};

export default App;
