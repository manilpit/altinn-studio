import update from 'immutability-helper';
import { Action, Reducer } from 'redux';
import { IUiConfig } from 'src/types/global';
import {
  ILayout,
} from '../';
import {
  IFetchFormLayoutFulfilled,
  IFetchFormLayoutRejected,
} from '../actions/fetch';
import * as ActionTypes from '../actions/types';
import { IUpdateFocusFulfilled, IUpdateFormLayout, IUpdateHiddenComponents } from '../actions/update';

export interface ILayoutState {
  layout: ILayout;
  error: Error;
  uiConfig: IUiConfig;
}

const initialState: ILayoutState = {
  layout: null,
  error: null,
  uiConfig: {
    focus: null,
    hiddenFields: [],
  },
};

const LayoutReducer: Reducer<ILayoutState> = (
  state: ILayoutState = initialState,
  action?: Action,
): ILayoutState => {
  if (!action) {
    return state;
  }

  switch (action.type) {
    case ActionTypes.FETCH_FORM_LAYOUT_FULFILLED: {
      const { layout } = action as IFetchFormLayoutFulfilled;
      return update<ILayoutState>(state, {
        layout: {
          $set: layout,
        },
        error: {
          $set: null,
        },
      });
    }
    case ActionTypes.FETCH_FORM_LAYOUT_REJECTED: {
      const { error } = action as IFetchFormLayoutRejected;
      return update<ILayoutState>(state, {
        error: {
          $set: error,
        },
      });
    }
    case ActionTypes.UPDATE_FOCUS_FULFUILLED: {
      const { focusComponentId } = action as IUpdateFocusFulfilled;
      return update<ILayoutState>(state, {
        uiConfig: {
          focus: {
            $set: focusComponentId,
          },
        },
      });
    }
    case ActionTypes.UPDATE_FORM_LAYOUT: {
      const { layoutElement, index } = action as IUpdateFormLayout;
      return update<ILayoutState>(state, {
        layout: {
          [index]: {
            $set: layoutElement,
          },
        },
      });
    }
    case ActionTypes.UPDATE_HIDDEN_COMPONENTS: {
      const { componentsToHide } = action as IUpdateHiddenComponents;
      return update<ILayoutState>(state, {
        uiConfig: {
          hiddenFields: {
            $set: componentsToHide,
          },
        },
      });
    }
    default: {
      return state;
    }
  }
};

export default LayoutReducer;