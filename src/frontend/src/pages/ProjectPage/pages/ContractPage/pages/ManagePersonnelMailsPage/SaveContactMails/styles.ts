import { makeStyles, createStyles } from '@equinor/fusion-react-styles';

const useStyles = makeStyles(() =>
    createStyles({
        container: {
            paddingRight: '1.5rem',
            display: 'flex',
            alignItems: 'center',
        },
        buttonContainer: {
            display: 'flex',
            alignItems: 'center',
        },
        title: {
            paddingLeft: '.5rem',
        },
        savingText: {
            fontSize: '14px',
            paddingRight: '1rem',
        },
    })
);

export default useStyles;
