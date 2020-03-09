import * as React from 'react';
import * as styles from './styles.less';
import Taskbar from './Taskbar';
import TableTextInput from './components/TableTextInput';
import TableBasePosition from './components/TableBasePositionPicker';
import TablePositionPicker from './components/TablePositionPicker';
import TablePersonPicker from './components/TablePersonPicker';
import TablePersonnelPicker from './components/TablePersonnelPicker';
import TableDatePicker from './components/TableDatePicker';

export type EditableTaleColumnItem =
    | 'TextInput'
    | 'PersonPicker'
    | 'PositionPicker'
    | 'BasePositionPicker'
    | 'PersonnelPicker'
    | 'DatePicker'
    | 'static';

export type EditableTaleColumn<T> = {
    accessor: (item: T) => any;
    item: EditableTaleColumnItem;
    label: string;
    accessKey: keyof T;
};

type EditableTableProps<T> = {
    formState: T[];
    setFormState: (newState: T[]) => void;
    columns: EditableTaleColumn<T>[];
    createDefaultState: () => T[];
    rowIdentifier: keyof T;
    isFetching?: boolean;
};

function EditableTable<T>({
    formState,
    setFormState,
    columns,
    createDefaultState,
    rowIdentifier,
    isFetching,
}: EditableTableProps<T>) {
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
        if (formState && formState.length <= 0) {
            onAddItem();
        }
    }, [formState]);

    const getTableComponent = React.useCallback(
        (column: EditableTaleColumn<T>, item: T) => {
            const defaultProps = {
                item,
                accessKey: column.accessKey,
                accessor: column.accessor,
                onChange,
                rowIdentifier,
                isFetching,
                columnLabel: column.label,
            };
            switch (column.item) {
                case 'TextInput':
                    return <TableTextInput {...defaultProps} />;
                case 'BasePositionPicker':
                    return <TableBasePosition {...defaultProps} />;
                case 'PositionPicker':
                    return <TablePositionPicker {...defaultProps} />;
                case 'PersonPicker':
                    return <TablePersonPicker {...defaultProps} />;
                case 'PersonnelPicker':
                    return <TablePersonnelPicker {...defaultProps} />;
                case 'DatePicker':
                    return <TableDatePicker {...defaultProps} />;
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
