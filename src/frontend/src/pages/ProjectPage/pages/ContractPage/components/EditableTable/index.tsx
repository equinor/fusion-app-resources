import * as React from 'react';
import useForm from '../../../../../../hooks/useForm';
import * as styles from './styles.less';
import Taskbar from './Taskbar';
import TableTextInput from './components/TableTextInput';
import TableBasePosition from './components/TableBasePositionPicker';
import TablePositionPicker from './components/TablePositionPicker';
import TablePersonPicker from './components/TablePersonPicker';

export type EditableTaleColumnItem =
    | 'TextInput'
    | 'PersonPicker'
    | 'PositionPicker'
    | 'BasePositionPicker'
    | 'static';

export type EditableTaleColumn<T> = {
    accessor: (item: T) => any;
    item: EditableTaleColumnItem;
    label: string;
    accessKey: keyof T;
};

type EditableTableProps<T> = {
    defaultState: T[] | null;
    columns: EditableTaleColumn<T>[];
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
        return !formState.some(state => !Boolean(Object.values(state).some(value => !!value)));
    }, []);

    const { formState, setFormState, isFormValid, isFormDirty } = useForm(
        createDefaultState,
        validateForm,
        defaultState
    );

    const onChange = (key: any, accessKey: keyof T, value: any) => {
        const updatedPersons = [...formState].map(stateItem =>
            stateItem[rowIdentifier] === key ? { ...stateItem, [accessKey]: value } : stateItem
        );
        setFormState(updatedPersons);
    };

    const onAddItem = React.useCallback(() => {
        const newStateItem = createDefaultState();
        setFormState([...formState, ...newStateItem]);
    }, [formState, createDefaultState]);

    React.useEffect(() => {
        if (defaultState && defaultState.length <= 0) {
            onAddItem();
        }
    }, [defaultState]);

    const getTableComponent = React.useCallback(
        (column: EditableTaleColumn<T>, item: T) => {
            const defaultProps = {
                item,
                accessKey: column.accessKey,
                accessor: column.accessor,
                onChange,
                rowIdentifier,
            };
            switch (column.item) {
                case 'TextInput':
                    return (
                        <TableTextInput
                            {...defaultProps}
                            disabled={false}
                            columnLabel={column.label}
                        />
                    );
                case 'BasePositionPicker':
                    return <TableBasePosition {...defaultProps} />;
                case 'PositionPicker':
                    return <TablePositionPicker {...defaultProps} columnLabel={column.label} />;
                case 'PersonPicker':
                    return <TablePersonPicker {...defaultProps} columnLabel={column.label} />;
                default:
                    return null;
            }
        },
        [onChange, rowIdentifier, onChange, rowIdentifier]
    );

    const tableHeader = React.useMemo(() => {
        return (
            <thead>
                <tr>
                    {columns.map(column => (
                        <th className={styles.header} key={column.label + 'header'}>
                            {column.label}
                        </th>
                    ))}
                </tr>
            </thead>
        );
    }, [columns]);

    const tableBody = React.useMemo(() => {
        return (
            <tbody>
                {formState.map((stateItem, index) => (
                    <tr key={`item-${index}`}>
                        {columns.map(column => (
                            <td className={styles.tableRowCell}>
                                {getTableComponent(column, stateItem)}
                            </td>
                        ))}
                    </tr>
                ))}
            </tbody>
        );
    }, [columns, formState]);

    return (
        <div className={styles.editableTable}>
            <Taskbar onAddItem={onAddItem} />
            <div className={styles.container}>
                <table>
                    {tableHeader}
                    {tableBody}
                </table>
            </div>
        </div>
    );
}

export default EditableTable;
