import { makeStyles, createStyles } from '@equinor/fusion-react-styles';
import { RadioButton } from '@equinor/fusion-components';

type RadioFilterProps<T> = {
    radioKey: keyof T;
    selectedKey: keyof T;
    onClick: (key: keyof T) => void;
    title: string;
};

const useStyles = makeStyles(() =>
    createStyles({
        container: {
            display: 'flex',
            alignItems: 'center',
            paddingLeft: '1rem',
            fontSize:"14px"
        },
    })
);

function RadioFilter<T>({ radioKey, onClick, selectedKey, title }: RadioFilterProps<T>) {
    const styles = useStyles();
    const onChange = () => onClick(radioKey);

    return (
        <div className={styles.container}>
            <RadioButton onChange={onChange} selected={radioKey === selectedKey} /> {title}
        </div>
    );
}

export default RadioFilter;
