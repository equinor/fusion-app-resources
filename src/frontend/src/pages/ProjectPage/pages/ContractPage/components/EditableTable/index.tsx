import * as React from 'react';
import useForm from '../../../../../../hooks/useForm';
import { DataTable, DataTableColumn } from '@equinor/fusion-components';
import useTableColumns from './useTableColumns';
import * as styles from './styles.less';
import Taskbar from './Taskbar';

export type EditableTaleColumnItem = 'TextInput' | 'PersonPicker' | 'PeoplePicker' | 'static';

export type EditableTaleColumns<T> = {
    accessor: (item: T) => string;
    item: EditableTaleColumnItem;
    label: string;
    accessKey: keyof T;
};

type EditableTableProps<T> = {
    defaultState: T[] | null;
    columns: EditableTaleColumns<T>[];
    createDefaultState: () => T[];
    rowIdentifier: keyof T;
};

function EditableTable<T>({
    defaultState,
    columns,
    createDefaultState,
    rowIdentifier,
}: EditableTableProps<T>) {

    const validateForm = React.useCallback((formState: T[]) => {
        return formState.some(state => Boolean(Object.values(state).some(value => !!value)));
    }, []);

    const {
        formState,
        setFormState,
        resetForm,
        formFieldSetter,
        setFormField,
        isFormValid,
        isFormDirty,
    } = useForm(createDefaultState, validateForm, defaultState);

    const onChange =(key: any, accessKey: keyof T, value: any) => {
 
            const updatedPersons = [...formState].map(stateItem =>
                stateItem[rowIdentifier] === key ? {...stateItem, [accessKey]: value} : stateItem
            );
            setFormState(updatedPersons);
            console.log(updatedPersons)
        }
    

    const onAddItem = React.useCallback(() => {
        const newStateItem = createDefaultState();
        setFormState([...formState, ...newStateItem]);
    }, [formState, createDefaultState]);

    React.useEffect(() => {
        if (defaultState === null) {
            onAddItem();
        }
    }, [defaultState]);

    const tableColumns = useTableColumns(columns, onChange, rowIdentifier);

    return (
        <div className={styles.editableTable}>
            <Taskbar onAddItem={onAddItem} />
            <DataTable
                columns={tableColumns}
                data={formState}
                isFetching={false}
                rowIdentifier={rowIdentifier}
            />
        </div>
    );
}

export default EditableTable;
