import { makeStyles, createStyles } from '@equinor/fusion-react-styles';

const useStyles = makeStyles(() =>
    createStyles({
        container: {
            paddingRight: '1.5rem',
        },
        buttonContainer: {
            display: 'flex',
            alignItems: 'center',
        },
        title: {
            paddingLeft: '.5rem',
        },
    })
);

export default useStyles;
