import { SagaIterator } from 'redux-saga';
import { fork } from 'redux-saga/effects';

import { watchGetApplicationMetadataSaga } from './get';

export default function*(): SagaIterator {
  yield fork(watchGetApplicationMetadataSaga);
}
