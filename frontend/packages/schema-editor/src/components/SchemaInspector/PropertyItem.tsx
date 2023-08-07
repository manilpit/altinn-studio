import type { ChangeEventHandler, KeyboardEvent } from 'react';
import React from 'react';
import { Checkbox, Select } from '@digdir/design-system-react';
import classes from './PropertyItem.module.css';
import { IconButton } from '../common/IconButton';
import { IconImage } from '../common/Icon';
import { getTypeOptions } from './helpers/options';
import type { FieldType } from '@altinn/schema-model';
import { useTranslation } from 'react-i18next';
import { useDatamodelMutation } from '@altinn/schema-editor/hooks/mutations';
import { useDatamodelQuery } from '@altinn/schema-editor/hooks/queries';
import { setRequired, setPropertyName } from '@altinn/schema-model';
import { NameField } from './NameField';

export interface IPropertyItemProps {
  fullPath: string;
  inputId: string;
  onChangeType: (path: string, type: FieldType) => void;
  onDeleteField: (path: string) => void;
  onEnterKeyPress: () => void;
  readOnly?: boolean;
  required?: boolean;
  type: FieldType;
}

export function PropertyItem({
  fullPath,
  inputId,
  onChangeType,
  onDeleteField,
  onEnterKeyPress,
  readOnly,
  required,
  type,
}: IPropertyItemProps) {
  const { data } = useDatamodelQuery();
  const { mutate } = useDatamodelMutation();

  const deleteHandler = () => onDeleteField?.(fullPath);

  const handleChangeNodeName = (newNodeName: string) => {
    mutate(
      setPropertyName(data, {
        path: fullPath,
        name: newNodeName,
      })
    );
  };

  const changeRequiredHandler: ChangeEventHandler<HTMLInputElement> = (e) =>
    mutate(
      setRequired(data, {
        path: fullPath,
        required: e.target.checked,
      })
    );

  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) =>
    e?.key === 'Enter' && onEnterKeyPress && onEnterKeyPress();

  const { t } = useTranslation();

  return (
    <>
      <div className={`${classes.nameInputCell} ${classes.gridItem}`}>
        <NameField
          id={inputId}
          disabled={readOnly}
          handleSave={handleChangeNodeName}
          onKeyDown={onKeyDown}
          pointer={fullPath}
          aria-label={t('schema_editor.field_name')}
        />
      </div>
      <div className={`${classes.typeSelectCell} ${classes.gridItem}`}>
        <Select
          hideLabel
          inputId={`${inputId}-typeselect`}
          label={t('schema_editor.type')}
          onChange={(fieldType) => onChangeType(fullPath, fieldType as FieldType)}
          options={getTypeOptions(t)}
          value={type}
        />
      </div>
      <span className={`${classes.requiredCheckCell} ${classes.gridItem}`}>
        <Checkbox
          checked={required ?? false}
          disabled={readOnly}
          hideLabel
          label={t('schema_editor.required')}
          name='checkedArray'
          onChange={changeRequiredHandler}
        />
      </span>
      <IconButton
        ariaLabel={t('schema_editor.delete_field')}
        icon={IconImage.Wastebucket}
        onClick={deleteHandler}
      />
    </>
  );
}
