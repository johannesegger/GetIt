import React from 'react';
import PropTypes from 'prop-types';
import { drawOps } from './../Canvas.fs';
import ReactAnimationFrame from 'react-animation-frame';

class Canvas extends React.Component {

    constructor(props) {
        super(props);
    }

    onAnimationFrame(timestamp, lastTimestamp) {
        this.props.onTick(timestamp, lastTimestamp);
    }

    componentDidMount() {
        drawOps(this.getContext(), this.props.drawOps);
        // this.refs.canvas.addEventListener("mousemove", this.mouveMove, false);
    }

    componentDidUpdate() {
        // if (this.props.isPlaying) {
            // drawOps(this.getContext(), this.props.drawOps);
            // For now we can tick directly because we are using
            // `Program.withReact` which use RAF internally
            // Would be nice if we could use RAF here directly for more control perhaps ?
            // this.props.onTick();
        // }
        drawOps(this.getContext(), this.props.drawOps);
    }

    getContext() {
        return this.refs.canvas.getContext("2d");
    }

    render() {
        return (
            <canvas id={this.props.id}
                width={this.props.width}
                onMouseMove={this.props.onMouseMove}
                height={this.props.height}
                style={this.props.style}
                ref="canvas">

            </canvas>
        );
    }
}

Canvas.propTypes = {
    id: PropTypes.string,
    width: PropTypes.number,
    height: PropTypes.number,
    drawOps: PropTypes.array,
    isPlaying: PropTypes.bool,
    onTick: PropTypes.func,
    onMouseMove: PropTypes.func,
    style : PropTypes.object
};

export default ReactAnimationFrame(Canvas);